using System.Security;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick.Infrastructure;

/// <summary>
/// Registers RightClick+ verbs under <c>HKCU\Software\Classes</c>: file verbs on all files (<c>*</c>),
/// Copy on files and folders (<c>AllFilesystemObjects</c>), Folder color on folders. Clicking a verb
/// makes Explorer start the helper CLI as a COM server (DelegateExecute) and hand it the whole
/// selection in one call, rather than starting the helper once per selected item.
/// </summary>
public sealed class ShellVerbService : IShellVerbService
{
    /// <summary>CLSID of the click handler that the helper CLI serves out of process.</summary>
    public static readonly Guid CommandHandlerClsid = new("A5C3FDB3-41C1-4D70-9FBE-EDCD61E58ABD");

    /// <summary>Value on each verb key naming the action the click handler runs, e.g. "copy name".</summary>
    public const string CommandValueName = "RightClickPlus.Command";

    private const string FilesShellRoot = @"Software\Classes\*\shell";
    private const string DirectoryShellRoot = @"Software\Classes\Directory\shell";
    private const string FilesAndFoldersShellRoot = @"Software\Classes\AllFilesystemObjects\shell";

    private const string FileToFolderKey = "RightClickPlus.FileToFolder";
    private const string FileNoteKey = "RightClickPlus.FileNote";
    private const string CopyKey = "RightClickPlus.Copy";
    private const string FolderColorKey = "RightClickPlus.FolderColor";

    // MultiSelectModel values. Without "Player", Windows hides a verb once more than 15 items are
    // selected; "Single" shows a verb only when exactly one item is selected.
    private const string AnyNumberOfItems = "Player";
    private const string OneItemOnly = "Single";

    private static readonly string[] FolderColors =
        { "red", "orange", "green", "mint", "teal", "aqua", "blue", "sky", "purple", "lavender", "pink" };

    private readonly ILogger<ShellVerbService> _logger;

    public ShellVerbService(ILogger<ShellVerbService> logger) => _logger = logger;

    private static string CliPath => Path.Combine(AppContext.BaseDirectory, "RightClick.Cli.exe");

    private static string IconPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "Assets", fileName);

    private static string HandlerKeyPath => $@"Software\Classes\CLSID\{CommandHandlerClsid:B}";

    private static string KeyName(ShellAction action) => action switch
    {
        ShellAction.FileToFolder => FileToFolderKey,
        ShellAction.FileNotes => FileNoteKey,
        ShellAction.CopyFileNames => CopyKey,
        ShellAction.FolderColor => FolderColorKey,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    private static string ShellRootFor(ShellAction action) => action switch
    {
        ShellAction.FolderColor => DirectoryShellRoot,
        ShellAction.CopyFileNames => FilesAndFoldersShellRoot,
        _ => FilesShellRoot,
    };

    // Up to 1.0.5, Copy was registered on files only (*). Kept so upgrades remove it instead of
    // leaving a duplicate Copy entry on files.
    private static string? LegacyShellRootFor(ShellAction action) =>
        action == ShellAction.CopyFileNames ? FilesShellRoot : null;

    public bool IsInstalled(ShellAction action) =>
        KeyExists(ShellRootFor(action), KeyName(action)) ||
        (LegacyShellRootFor(action) is { } legacy && KeyExists(legacy, KeyName(action)));

    private static bool KeyExists(string rootPath, string keyName)
    {
        using var root = Registry.CurrentUser.OpenSubKey(rootPath);
        using var key = root?.OpenSubKey(keyName);
        return key is not null;
    }

    public void Install(ShellAction action)
    {
        RegisterClickHandler();
        switch (action)
        {
            case ShellAction.FileToFolder:
                InstallSimpleVerb(FileToFolderKey, "File to Folder", "file-to-folder.ico", "file-to-folder", AnyNumberOfItems);
                break;
            case ShellAction.FileNotes:
                InstallSimpleVerb(FileNoteKey, "File Note", "file-notes.ico", "file-note", OneItemOnly);
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
        DeleteKey(ShellRootFor(action), KeyName(action));
        if (LegacyShellRootFor(action) is { } legacy)
        {
            DeleteKey(legacy, KeyName(action));
        }
        _logger.LogInformation("Uninstalled shell verb for {Action}.", action);
    }

    private static void DeleteKey(string rootPath, string keyName)
    {
        using var root = Registry.CurrentUser.CreateSubKey(rootPath, writable: true);
        root.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
    }

    /// <summary>
    /// Registers the helper CLI as the COM server Explorer starts when a verb is clicked. Always per
    /// user; also machine-wide when this process is elevated, because Windows ignores per-user COM
    /// registrations for elevated processes, and with UAC turned off Explorer itself runs elevated.
    /// </summary>
    private void RegisterClickHandler()
    {
        WriteHandler(Registry.CurrentUser);
        try
        {
            WriteHandler(Registry.LocalMachine);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            // Not elevated, so Explorer isn't either: the per-user registration is all it needs.
            if (IsElevated())
            {
                _logger.LogError(ex, "Couldn't register the click handler machine-wide; menu entries won't work while UAC is off.");
            }
        }
    }

    private static void WriteHandler(RegistryKey hive)
    {
        using var server = hive.CreateSubKey($@"{HandlerKeyPath}\LocalServer32", writable: true);
        server.SetValue(null, $"\"{CliPath}\"");
    }

    /// <summary>Removes the click handler registration (per user, and machine-wide when allowed).</summary>
    public void RemoveClickHandler()
    {
        Registry.CurrentUser.DeleteSubKeyTree(HandlerKeyPath, throwOnMissingSubKey: false);
        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(HandlerKeyPath, throwOnMissingSubKey: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            // Not elevated; a machine-wide registration can only exist if an elevated run made it.
        }
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Points a verb at the click handler and records which action it runs.</summary>
    private static void SetHandler(RegistryKey verb, string command)
    {
        verb.SetValue(CommandValueName, command);
        using var cmd = verb.CreateSubKey("command", writable: true);
        cmd.SetValue("DelegateExecute", CommandHandlerClsid.ToString("B"));
    }

    private static void InstallSimpleVerb(string keyName, string label, string iconFile, string command, string selectionModel)
    {
        using var root = Registry.CurrentUser.CreateSubKey(FilesShellRoot, writable: true);
        using var verb = root.CreateSubKey(keyName, writable: true);
        verb.SetValue("MUIVerb", label);
        verb.SetValue("Icon", IconPath(iconFile));
        verb.SetValue("MultiSelectModel", selectionModel);
        SetHandler(verb, command);
    }

    /// <summary>
    /// A cascading "Copy" submenu built from nested shell subkeys. An empty SubCommands value
    /// tells the shell to enumerate the child verbs under the parent's "shell" subkey. Registered on
    /// files and folders, so it also works on folders and on mixed selections.
    /// </summary>
    private static void InstallCopySubmenu()
    {
        using var root = Registry.CurrentUser.CreateSubKey(FilesAndFoldersShellRoot, writable: true);
        using var parent = root.CreateSubKey(CopyKey, writable: true);
        // "&&" shows a literal "&": a single "&" in menu text marks a keyboard shortcut and is hidden.
        parent.SetValue("MUIVerb", "Copy File && Folder Names");
        parent.SetValue("Icon", IconPath("copy-file-names.ico"));
        parent.SetValue("SubCommands", string.Empty);
        parent.SetValue("MultiSelectModel", AnyNumberOfItems);

        using var shell = parent.CreateSubKey("shell", writable: true);

        AddChild(shell, "01name", "Copy name", "copy name");
        AddChild(shell, "02noext", "Copy name without extension", "copy name-noext");
        AddChild(shell, "03path", "Copy full path", "copy path");
        AddChild(shell, "04quoted", "Copy full path (quoted)", "copy path-quoted");
    }

    /// <summary>
    /// A cascading "Folder color" submenu on the folder class: one swatch per color (each shows its
    /// colored-folder icon) plus a Reset item.
    /// </summary>
    private static void InstallFolderColorSubmenu()
    {
        using var root = Registry.CurrentUser.CreateSubKey(DirectoryShellRoot, writable: true);
        using var parent = root.CreateSubKey(FolderColorKey, writable: true);
        parent.SetValue("MUIVerb", "Folder Color");
        parent.SetValue("Icon", IconPath("folder-blue.ico"));
        parent.SetValue("SubCommands", string.Empty);
        parent.SetValue("MultiSelectModel", AnyNumberOfItems);

        using var shell = parent.CreateSubKey("shell", writable: true);

        var index = 1;
        foreach (var color in FolderColors)
        {
            var label = char.ToUpperInvariant(color[0]) + color[1..];
            AddChild(shell, $"{index:00}{color}", label, $"folder-color {color}", $"folder-{color}.ico");
            index++;
        }
        AddChild(shell, "99reset", "Reset color", "folder-color reset", "AppIcon.ico");
    }

    private static void AddChild(RegistryKey shell, string keyName, string label, string command, string? iconFile = null)
    {
        using var child = shell.CreateSubKey(keyName, writable: true);
        child.SetValue("MUIVerb", label);
        if (iconFile is not null)
        {
            child.SetValue("Icon", IconPath(iconFile));
        }
        SetHandler(child, command);
    }
}
