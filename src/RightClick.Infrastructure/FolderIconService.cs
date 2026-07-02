using System.Runtime.InteropServices;

namespace RightClick.Infrastructure;

/// <summary>
/// Applies (or clears) a custom icon on a folder via desktop.ini. This mirrors, exactly, what the
/// commercial tool FolderIco does — verified by inspecting a folder it coloured and by capturing the
/// shell notifications it broadcasts. The recipe that colours INSTANTLY on patched Windows 11:
///   1. A UNIQUE icon filename per apply ("RightClickPlus_xxxx.ico"). A new path each time sidesteps
///      Windows' by-path icon-bitmap cache, so the new colour is never served stale.
///   2. A RELATIVE IconResource in desktop.ini (just the filename); absolute/remote paths are blocked
///      by the June-2026 "remote paths in icons" trust check.
///   3. The folder marked ReadOnly ONLY (NOT System). ReadOnly is enough for Explorer to read
///      desktop.ini; adding System makes Explorer treat it as a protected system folder and take a
///      slow (~1 minute) refresh path — that was the cause of our lag.
///   4. Exactly ONE notification: SHChangeNotify(SHCNE_UPDATEITEM) on the folder. No parent refresh,
///      no icon-file notify, and crucially NO view "F5" — a re-enumeration re-caches the old icon and
///      undoes the update, which is what broke earlier attempts.
/// </summary>
public sealed class FolderIconService
{
    private const string IconPrefix = "RightClickPlus";
    private const string IconGlob = "RightClickPlus*.ico";

    /// <summary>Copies an existing .ico file into the folder and applies it.</summary>
    public void Apply(string folder, string icoSourcePath) =>
        ApplyCore(folder, dest => File.Copy(icoSourcePath, dest, overwrite: true));

    /// <summary>Writes freshly-rendered .ico bytes into the folder and applies it.</summary>
    public void Apply(string folder, byte[] icoBytes) =>
        ApplyCore(folder, dest => File.WriteAllBytes(dest, icoBytes));

    private void ApplyCore(string folder, Action<string> write)
    {
        // Unique name per apply → unique path → Windows can't serve a stale cached bitmap.
        var iconName = $"{IconPrefix}_{Guid.NewGuid().ToString("N")[..8]}.ico";
        var destIco = Path.Combine(folder, iconName);
        write(destIco);
        File.SetAttributes(destIco, FileAttributes.Hidden | FileAttributes.System);

        // RELATIVE reference (just the filename); absolute paths are blocked by the June-2026 trust check.
        var ini = Path.Combine(folder, "desktop.ini");
        if (File.Exists(ini))
        {
            File.SetAttributes(ini, FileAttributes.Normal);
        }
        File.WriteAllText(ini, $"[.ShellClassInfo]\r\nIconResource={iconName},0\r\nConfirmFileOp=0\r\n");
        File.SetAttributes(ini, FileAttributes.Hidden | FileAttributes.System);

        // ReadOnly ONLY (never System) — matches FolderIco and keeps Explorer on the fast refresh path.
        var di = new DirectoryInfo(folder);
        di.Attributes |= FileAttributes.ReadOnly;
        if (di.Attributes.HasFlag(FileAttributes.System))
        {
            di.Attributes &= ~FileAttributes.System; // undo System left by earlier versions
        }

        // Delete previous icons ONLY now that desktop.ini already points at the new one, so at no
        // point does the cached desktop.ini reference a file that no longer exists (that flashed the
        // default folder icon). Keep the one we just wrote.
        RemoveIconFiles(folder, keep: iconName);

        Notify(folder);
    }

    /// <summary>Removes the custom icon and restores the default folder appearance.</summary>
    public void Reset(string folder)
    {
        RemoveIconFiles(folder, keep: null);
        var ini = Path.Combine(folder, "desktop.ini");
        if (File.Exists(ini))
        {
            File.SetAttributes(ini, FileAttributes.Normal);
            File.Delete(ini);
        }
        new DirectoryInfo(folder).Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.System);
        Notify(folder);
    }

    private static void RemoveIconFiles(string folder, string? keep)
    {
        foreach (var file in Directory.EnumerateFiles(folder, IconGlob))
        {
            if (keep is not null && string.Equals(Path.GetFileName(file), keep, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch
            {
                // A locked leftover icon shouldn't abort applying the new one.
            }
        }
    }

    // ---- Shell refresh ----

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_UPDATEITEM = 0x2000;
    private const uint SHCNF_PATHW = 0x0005;
    private const uint SHCNF_FLUSH = 0x1000;

    /// <summary>
    /// Tells Explorer the folder changed so it repaints the icon — a single SHCNE_UPDATEITEM on the
    /// folder, exactly and only what FolderIco broadcasts (captured via SHChangeNotifyRegister).
    /// Deliberately NOT followed by a view refresh: re-enumerating the view re-caches the stale icon
    /// and undoes this. SHCNF_FLUSH delivers synchronously before this short-lived CLI process exits.
    /// </summary>
    private static void Notify(string folder)
    {
        var ptr = Marshal.StringToHGlobalUni(folder.TrimEnd('\\'));
        try { SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, ptr, IntPtr.Zero); }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
