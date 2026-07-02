using RightClick.Core.Models;

namespace RightClick.Core.Services;

/// <summary>
/// Detects and switches the Windows 11 modern/classic context menu by managing the
/// well-known classic-menu CLSID under HKCU, then restarting Explorer to apply.
/// </summary>
public interface IContextMenuTweakService
{
    /// <summary>Reads the current context-menu state from the registry and OS version.</summary>
    ContextMenuState GetState();

    /// <summary>
    /// Forces the classic context menu (writes the CLSID tweak) or restores the modern menu
    /// (removes it). No-op on Windows 10 where the menu is already classic.
    /// </summary>
    /// <param name="mode">Target mode — <see cref="MenuMode.Classic"/> or <see cref="MenuMode.Modern"/>.</param>
    /// <param name="restartExplorer">When true, restarts Explorer so the change takes effect immediately.</param>
    Task SetModeAsync(MenuMode mode, bool restartExplorer = true, CancellationToken cancellationToken = default);

    /// <summary>Restarts the Windows Explorer shell so registry changes take effect.</summary>
    Task RestartExplorerAsync(CancellationToken cancellationToken = default);
}
