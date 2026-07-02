namespace RightClick.Core.Models;

/// <summary>
/// A snapshot of the current Windows context-menu configuration.
/// </summary>
public sealed record ContextMenuState
{
    /// <summary>The currently active menu mode.</summary>
    public required MenuMode Mode { get; init; }

    /// <summary>True when running on Windows 11 (build 22000+), where the modern/classic toggle applies.</summary>
    public required bool IsWindows11 { get; init; }

    /// <summary>Friendly OS description, e.g. "Windows 11 (build 26100)".</summary>
    public required string OsDescription { get; init; }
}
