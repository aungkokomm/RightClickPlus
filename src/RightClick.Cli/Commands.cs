using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Extensions.Logging.Abstractions;
using RightClick.Core.Models;
using RightClick.Infrastructure;

namespace RightClick.Cli;

internal static class Commands
{
    private const string AppTitle = "RightClick+";

    // ---- Verb registration (used by the installer and for headless testing) ----

    public static void Register(string actionName, bool install)
    {
        if (!TryParseAction(actionName, out var action))
        {
            CliLog.Error($"Register: unknown action '{actionName}'.");
            return;
        }

        try
        {
            var svc = new ShellVerbService(NullLogger<ShellVerbService>.Instance);
            if (install)
            {
                svc.Install(action);
            }
            else
            {
                svc.Uninstall(action);
            }
        }
        catch (Exception ex)
        {
            CliLog.Error($"Register({actionName}, {install}) failed", ex);
        }
    }

    /// <summary>
    /// Removes every RightClick+ shell verb from HKCU. Called by the uninstaller so no orphaned
    /// (broken) context-menu entries are left pointing at the deleted CLI. Uninstall uses
    /// DeleteSubKeyTree(throwOnMissingSubKey: false), so this is safe when a verb isn't installed.
    /// </summary>
    public static void UnregisterAll()
    {
        try
        {
            var svc = new ShellVerbService(NullLogger<ShellVerbService>.Instance);
            foreach (ShellAction action in Enum.GetValues<ShellAction>())
            {
                svc.Uninstall(action);
            }
        }
        catch (Exception ex)
        {
            CliLog.Error("UnregisterAll failed", ex);
        }
    }

    private static bool TryParseAction(string name, out ShellAction action)
    {
        switch (name.ToLowerInvariant())
        {
            case "file-to-folder": action = ShellAction.FileToFolder; return true;
            case "file-note":
            case "file-notes": action = ShellAction.FileNotes; return true;
            case "copy":
            case "copy-file-names": action = ShellAction.CopyFileNames; return true;
            case "folder-color": action = ShellAction.FolderColor; return true;
            default: action = default; return false;
        }
    }

    private static AppSettings LoadSettings()
    {
        var svc = new SettingsService(NullLogger<SettingsService>.Instance);
        svc.LoadAsync().GetAwaiter().GetResult();
        return svc.Current;
    }

    // ---- File To Folder (per-file; each file into its own folder) ----

    public static void FileToFolder(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return; // folders/drives are ignored
            }

            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            // Self-heal: remove any ".rcpmove" staging file a previously-killed move left behind.
            SweepStaleStaging(dir);

            var fileName = Path.GetFileName(path);
            var folderName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = fileName; // dotfiles / extensionless names (".gitignore", "LICENSE")
            }

            // An extensionless file (e.g. "LICENSE", "Makefile", ".gitignore") wants a folder of the
            // SAME name it currently occupies. Windows can't hold a file and folder under one name, so
            // the generic clash logic below would bump it to "name (2)". Instead stage the file aside,
            // create the folder, then move it back in — yielding the natural "LICENSE\LICENSE".
            if (string.Equals(Path.Combine(dir, folderName), path, StringComparison.OrdinalIgnoreCase))
            {
                var staged = UniqueIfNeeded(path + ".rcpmove", isDirectory: false);
                File.Move(path, staged);
                Directory.CreateDirectory(path); // the name is free now that the file is staged
                File.Move(staged, Path.Combine(path, fileName));
                return;
            }

            // Resolve the destination folder, tolerating name clashes.
            var targetDir = ResolveTargetDir(dir, folderName);
            Directory.CreateDirectory(targetDir);

            var dest = Path.Combine(targetDir, fileName);
            if (!string.Equals(dest, path, StringComparison.OrdinalIgnoreCase))
            {
                dest = UniqueIfNeeded(dest, isDirectory: false);
                File.Move(path, dest);
            }
        }
        catch (Exception ex)
        {
            CliLog.Error($"FileToFolder failed for '{path}'", ex);
            MessageBox.Show(
                $"Couldn't move \"{Path.GetFileName(path)}\" into a folder.\r\n\r\n{ex.Message}",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Deletes stale "*.rcpmove" staging files (from a move that was killed mid-flight) in the folder.
    /// Only files older than a minute are removed, so a concurrent in-flight staging file is never touched.
    /// </summary>
    private static void SweepStaleStaging(string dir)
    {
        try
        {
            foreach (var stale in Directory.EnumerateFiles(dir, "*.rcpmove"))
            {
                try
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(stale) > TimeSpan.FromSeconds(60))
                    {
                        File.SetAttributes(stale, FileAttributes.Normal);
                        File.Delete(stale);
                    }
                }
                catch { /* best effort */ }
            }
        }
        catch { /* best effort */ }
    }

    // ---- File Note (single file only) ----

    public static void FileNote(IReadOnlyList<string> files)
    {
        try
        {
            if (files.Count == 0)
            {
                return;
            }

            if (files.Count > 1)
            {
                MessageBox.Show(
                    "Please select a single file to add a note.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var file = files[0];
            var settings = LoadSettings();
            var ext = settings.NoteFormat == NoteFormat.Markdown ? ".md" : ".txt";
            // Replace the original extension rather than appending it: "report.exe" -> "report.txt".
            var notePath = Path.ChangeExtension(file, ext);

            if (!File.Exists(notePath))
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var name = Path.GetFileName(file);
                var header = settings.NoteFormat == NoteFormat.Markdown
                    ? $"# {name}\r\n\r\n_{stamp}_\r\n\r\n"
                    : $"{name}\r\n{stamp}\r\n\r\n";
                File.WriteAllText(notePath, header);
            }

            OpenInEditor(notePath, settings.NoteEditor);
        }
        catch (Exception ex)
        {
            CliLog.Error("FileNote failed", ex);
        }
    }

    private static void OpenInEditor(string notePath, NoteEditor editor)
    {
        if (editor == NoteEditor.BuiltIn && TryOpenBuiltInEditor(notePath))
        {
            return;
        }

        OpenInNotepad(notePath);
    }

    /// <summary>
    /// Opens the note in RightClick+'s own editor window by launching the app with a "note" argument.
    /// Launching our own self-contained app loads its matching WinAppSDK DLLs from its own folder, so
    /// there is no version clash (unlike launching the Store Notepad).
    /// </summary>
    private static bool TryOpenBuiltInEditor(string notePath)
    {
        try
        {
            var app = Path.Combine(AppContext.BaseDirectory, "RightClick.App.exe");
            if (!File.Exists(app))
            {
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = app,
                Arguments = $"note \"{notePath}\"",
                UseShellExecute = false,
            });
            return true;
        }
        catch (Exception ex)
        {
            CliLog.Error("Built-in editor launch failed; falling back to Notepad.", ex);
            return false;
        }
    }

    /// <summary>
    /// Opens the note in Notepad. The modern Store Notepad is a Windows App SDK app; resolving the
    /// bare name "notepad.exe" hits its execution alias, whose loader then collides with our adjacent
    /// self-contained WinAppSDK DLLs and fails ("Microsoft.UI.Windowing.Core.dll was not found").
    /// Launching the classic Win32 Notepad by full path (System32\notepad.exe) has no WinAppSDK
    /// dependency, so it opens cleanly regardless of our folder.
    /// </summary>
    private static void OpenInNotepad(string notePath)
    {
        var notepad = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");
        Process.Start(new ProcessStartInfo
        {
            FileName = File.Exists(notepad) ? notepad : "notepad.exe",
            Arguments = $"\"{notePath}\"",
            // CreateProcess on the full path (not ShellExecute) so the launch is NOT redirected to the
            // Store Notepad execution alias, which fails to load against our adjacent WinAppSDK DLLs.
            UseShellExecute = false,
        });
    }

    // ---- Copy file names / paths ----

    public static void Copy(string variant, IReadOnlyList<string> files)
    {
        try
        {
            if (files.Count == 0)
            {
                return;
            }

            var settings = LoadSettings();
            var formatted = files.Select(f => Format(variant, f));
            var sep = settings.CopySeparator == CopySeparator.Comma ? ", " : "\r\n";
            if (!Clip.SetText(string.Join(sep, formatted)))
            {
                MessageBox.Show(
                    "Couldn't copy to the clipboard. It may be locked by another program; please try again.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            CliLog.Error($"Copy '{variant}' failed", ex);
        }
    }

    private static string Format(string variant, string path) => variant switch
    {
        "name" => Path.GetFileName(path),
        "name-noext" => Path.GetFileNameWithoutExtension(path),
        "path" => path,
        "path-quoted" => $"\"{path}\"",
        _ => path,
    };

    // ---- Folder color (per-folder; sets a colored icon via desktop.ini) ----

    public static void FolderColor(string color, string folder)
    {
        try
        {
            if (!Directory.Exists(folder))
            {
                // A color verb should only ever fire on a folder; if the path is empty or wrong
                // the launch token is the culprit. Surface it instead of failing silently.
                MessageBox.Show(
                    $"Couldn't find the folder to colour.\r\n\r\nPath received: \"{folder}\"",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.Equals(color, "reset", StringComparison.OrdinalIgnoreCase))
            {
                FolderIcons.Reset(folder);
            }
            else
            {
                var src = Path.Combine(AppContext.BaseDirectory, "Assets", $"folder-{color}.ico");
                if (!File.Exists(src))
                {
                    throw new FileNotFoundException(
                        $"The colour icon is missing from the install.\r\nExpected: {src}");
                }
                FolderIcons.Apply(folder, src);
            }
            // Success is silent — the recolored folder is the feedback. Only failures pop a message.
        }
        catch (Exception ex)
        {
            CliLog.Error($"FolderColor '{color}' on '{folder}' failed", ex);
            MessageBox.Show(
                $"Couldn't apply the folder colour.\r\n\r\n{ex.Message}",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Shared apply/reset recipe (relative path + System|ReadOnly + shell refresh).</summary>
    private static readonly FolderIconService FolderIcons = new();

    /// <summary>
    /// Picks the folder a file should move into. Merges into an existing folder of the same name;
    /// if a *file* already owns that name (e.g. moving "x.exe.txt" next to a real "x.exe"), falls back
    /// to a non-colliding "name (n)" folder so directory creation never fails.
    /// </summary>
    private static string ResolveTargetDir(string dir, string folderName)
    {
        var candidate = Path.Combine(dir, folderName);
        if (Directory.Exists(candidate))
        {
            return candidate; // merge
        }
        if (!File.Exists(candidate))
        {
            return candidate; // free name
        }

        for (var i = 2; i < 1000; i++)
        {
            var next = Path.Combine(dir, $"{folderName} ({i})");
            if (!File.Exists(next) && !Directory.Exists(next))
            {
                return next;
            }
        }
        return candidate;
    }

    private static string UniqueIfNeeded(string candidate, bool isDirectory)
    {
        bool Exists(string p) => isDirectory ? Directory.Exists(p) : File.Exists(p);
        if (!Exists(candidate))
        {
            return candidate;
        }

        var dir = Path.GetDirectoryName(candidate)!;
        var name = isDirectory ? Path.GetFileName(candidate) : Path.GetFileNameWithoutExtension(candidate);
        var ext = isDirectory ? string.Empty : Path.GetExtension(candidate);

        for (var i = 2; i < 1000; i++)
        {
            var next = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!Exists(next))
            {
                return next;
            }
        }
        return candidate;
    }
}
