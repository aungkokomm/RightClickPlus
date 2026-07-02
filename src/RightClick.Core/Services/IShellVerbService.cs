using RightClick.Core.Models;

namespace RightClick.Core.Services;

/// <summary>
/// Installs and removes RightClick+'s own context-menu verbs under HKCU\Software\Classes,
/// pointing them at the helper CLI executable. No admin required.
/// </summary>
public interface IShellVerbService
{
    /// <summary>True if the given action's verb is currently registered.</summary>
    bool IsInstalled(ShellAction action);

    /// <summary>Registers the verb (and submenu, for Copy) so it appears in the context menu.</summary>
    void Install(ShellAction action);

    /// <summary>Removes the verb from the context menu.</summary>
    void Uninstall(ShellAction action);
}
