using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick.Infrastructure;

/// <summary>
/// Registers RightClick+ verbs under <c>HKCU\Software\Classes\*\shell</c> so they appear
/// on all files. Commands point at the helper CLI next to the app executable. No admin needed.
/// </summary>
public sealed class ShellVerbService : IShellVerbService
{
    // File verbs live under the "all files" class; Folder color lives under the folder class.
    private const string FilesShellRoot = @"Software\Classes\*\shell";
    private const string DirectoryShellRoot = @"Software\Classes\Directory\shell";

    private const string FileToFolderKey = "RightClickPlus.FileToFolder";
    private const string FileNoteKey = "RightClickPlus.FileNote";
    private const string CopyKey = "RightClickPlus.Copy";
    private const string FolderColorKey = "RightClickPlus.FolderColor";

    private static readonly string[] FolderColors =
    {
        "red", "orange", "green", "mint", "teal", "aqua", "blue", "sky", "purple", "lavender", "pink",
        "indigo", "lime", "wine", "periwinkle", "raspberry", "cocoa", "charcoal", "slate",
    };

    private readonly ILogger<ShellVerbService> _logger;

    public ShellVerbService(ILogger<ShellVerbService> logger) => _logger = logger;

    private static string CliPath => Path.Combine(AppContext.BaseDirectory, "RightClick.Cli.exe");

    private static string IconPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "Assets", fileName);

    private static string KeyName(ShellAction action) => action switch
    {
        ShellAction.FileToFolder => FileToFolderKey,
        ShellAction.FileNotes => FileNoteKey,
        ShellAction.CopyFileNames => CopyKey,
        ShellAction.FolderColor => FolderColorKey,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    // Folder color attaches to folders; everything else to all files.
    private static string ShellRootFor(ShellAction action) =>
        action == ShellAction.FolderColor ? DirectoryShellRoot : FilesShellRoot;

    public bool IsInstalled(ShellAction action)
    {
        using var root = Registry.CurrentUser.OpenSubKey(ShellRootFor(action));
        using var key = root?.OpenSubKey(KeyName(action));
        return key is not null;
    }

    public void Install(ShellAction action)
    {
        switch (action)
        {
            case ShellAction.FileToFolder:
                InstallSimpleVerb(FileToFolderKey, "Move to folder", "file-to-folder.ico", "file-to-folder");
                break;
            case ShellAction.FileNotes:
                InstallSimpleVerb(FileNoteKey, "File note", "file-notes.ico", "file-note");
                break;
            case ShellAction.CopyFileNames:
                InstallCopySubmenu();
                break;
            case ShellAction.FolderColor:
                InstallFolderColorSubmenu();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
        _logger.LogInformation("Installed shell verb for {Action}.", action);
    }

    public void Uninstall(ShellAction action)
    {
        using var root = Registry.CurrentUser.CreateSubKey(ShellRootFor(action), writable: true);
        root.DeleteSubKeyTree(KeyName(action), throwOnMissingSubKey: false);
        _logger.LogInformation("Uninstalled shell verb for {Action}.", action);
    }

    /// <summary>A single verb: label + icon + a command that passes the clicked file as %1.</summary>
    private void InstallSimpleVerb(string keyName, string label, string iconFile, string cliVerb)
    {
        using var root = Registry.CurrentUser.CreateSubKey(FilesShellRoot, writable: true);
        using var verb = root.CreateSubKey(keyName, writable: true);
        verb.SetValue("MUIVerb", label);
        verb.SetValue("Icon", IconPath(iconFile));

        using var command = verb.CreateSubKey("command", writable: true);
        command.SetValue(null, $"\"{CliPath}\" {cliVerb} \"%1\"");
    }

    /// <summary>
    /// A cascading "Copy" submenu built from nested shell subkeys. An empty SubCommands value
    /// tells the shell to enumerate the child verbs under the parent's "shell" subkey.
    /// </summary>
    private void InstallCopySubmenu()
    {
        using var root = Registry.CurrentUser.CreateSubKey(FilesShellRoot, writable: true);
        using var parent = root.CreateSubKey(CopyKey, writable: true);
        parent.SetValue("MUIVerb", "Copy file name(s)");
        parent.SetValue("Icon", IconPath("copy-file-names.ico"));
        parent.SetValue("SubCommands", string.Empty);

        using var shell = parent.CreateSubKey("shell", writable: true);

        AddCopyChild(shell, "01name", "Copy file name", "copy name");
        AddCopyChild(shell, "02noext", "Copy name without extension", "copy name-noext");
        AddCopyChild(shell, "03path", "Copy full path", "copy path");
        AddCopyChild(shell, "04quoted", "Copy full path (quoted)", "copy path-quoted");
    }

    private void AddCopyChild(RegistryKey shell, string keyName, string label, string cliArgs)
    {
        using var child = shell.CreateSubKey(keyName, writable: true);
        child.SetValue("MUIVerb", label);
        using var command = child.CreateSubKey("command", writable: true);
        command.SetValue(null, $"\"{CliPath}\" {cliArgs} \"%1\"");
    }

    /// <summary>
    /// A cascading "Folder color" submenu on the folder class: one swatch per color (each shows its
    /// colored-folder icon) plus a Reset item.
    /// </summary>
    private void InstallFolderColorSubmenu()
    {
        using var root = Registry.CurrentUser.CreateSubKey(DirectoryShellRoot, writable: true);
        using var parent = root.CreateSubKey(FolderColorKey, writable: true);
        parent.SetValue("MUIVerb", "Folder color");
        parent.SetValue("Icon", IconPath("folder-blue.ico"));
        parent.SetValue("SubCommands", string.Empty);

        using var shell = parent.CreateSubKey("shell", writable: true);

        // Reset first ("00" sorts before the colours) so it's always visible at the top — with 19
        // colours the submenu is taller than the screen, and a Reset at the bottom falls off it.
        AddColorChild(shell, "00reset", "Reset color", "folder-color reset", "AppIcon.ico");

        var index = 1;
        foreach (var color in FolderColors)
        {
            var label = char.ToUpperInvariant(color[0]) + color[1..];
            AddColorChild(shell, $"{index:00}{color}", label, $"folder-color {color}", $"folder-{color}.ico");
            index++;
        }
    }

    private void AddColorChild(RegistryKey shell, string keyName, string label, string cliArgs, string iconFile)
    {
        using var child = shell.CreateSubKey(keyName, writable: true);
        child.SetValue("MUIVerb", label);
        child.SetValue("Icon", IconPath(iconFile));
        using var command = child.CreateSubKey("command", writable: true);
        // Directory verbs receive the clicked folder via %V (the token Windows' own folder
        // verbs use, e.g. "Open in Terminal"). %1 is unreliable for the Directory class.
        command.SetValue(null, $"\"{CliPath}\" {cliArgs} \"%V\"");
    }
}
