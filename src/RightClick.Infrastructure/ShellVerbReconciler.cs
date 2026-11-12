using System.Runtime.InteropServices;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick.Infrastructure;

/// <summary>
/// Brings the registered right-click verbs in line with the SAVED settings, the source of truth: each
/// enabled feature is registered fresh (so it always matches this build's menu, icons and paths), and
/// each disabled feature is removed. Runs when the app starts and right after installing, so an upgrade
/// takes effect straight away instead of waiting for the app to be opened.
/// </summary>
public static class ShellVerbReconciler
{
    /// <param name="onError">Receives each failure; one feature failing doesn't stop the others.</param>
    public static void Reconcile(IShellVerbService shell, AppSettings settings, Action<string, Exception> onError)
    {
        foreach (var action in Enum.GetValues<ShellAction>())
        {
            try
            {
                if (IsEnabled(settings, action))
                {
                    // Clean re-write: remove any stale/old registration, then register fresh.
                    shell.Uninstall(action);
                    shell.Install(action);
                }
                else if (shell.IsInstalled(action))
                {
                    shell.Uninstall(action);
                }
            }
            catch (Exception ex)
            {
                onError($"Couldn't update the right-click entry for {action}.", ex);
            }
        }

        // Tell the shell associations changed and flush the icon cache, so updated menu
        // icons (e.g. new folder-colour art) replace any version Explorer cached by path.
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
    }

    private static bool IsEnabled(AppSettings s, ShellAction action) => action switch
    {
        ShellAction.FileToFolder => s.FileToFolderEnabled,
        ShellAction.FileNotes => s.FileNotesEnabled,
        ShellAction.CopyFileNames => s.CopyFileNamesEnabled,
        ShellAction.FolderColor => s.FolderColorEnabled,
        _ => false,
    };

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_FLUSH = 0x1000;
}
