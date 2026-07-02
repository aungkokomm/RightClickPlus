namespace RightClick.Core.Models;

/// <summary>
/// Which Windows Explorer context menu style is active.
/// </summary>
public enum MenuMode
{
    /// <summary>The Windows 11 modern (compact) context menu.</summary>
    Modern,

    /// <summary>The classic (full) context menu — forced on Windows 11 via the CLSID tweak.</summary>
    Classic,

    /// <summary>Windows 10 (or earlier) where the classic menu is native and the tweak does not apply.</summary>
    NativeClassic,
}
