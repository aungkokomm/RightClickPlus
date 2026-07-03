using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace RightClick_App;

/// <summary>Shared window helpers: DPI-aware sizing and executable-relative asset paths.</summary>
internal static class WindowHelpers
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    /// <summary>Resizes the window to a logical size, scaled for the monitor's DPI.</summary>
    public static void ResizeScaled(this AppWindow window, int logicalWidth, int logicalHeight)
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(window.Id);
        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi > 0 ? dpi / 96.0 : 1.0;
        window.Resize(new SizeInt32((int)(logicalWidth * scale), (int)(logicalHeight * scale)));
    }

    /// <summary>
    /// Sizes the window to a fixed logical width and a content-driven logical height (both DPI-scaled),
    /// capped to the monitor's work area so it never runs off-screen. Used to fit the main window
    /// snugly to its content instead of leaving empty space at the bottom.
    /// </summary>
    public static void FitToContent(this AppWindow window, int logicalWidth, double logicalHeight)
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(window.Id);
        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi > 0 ? dpi / 96.0 : 1.0;
        var work = DisplayArea.GetFromWindowId(window.Id, DisplayAreaFallback.Primary).WorkArea;

        var physicalWidth = (int)(logicalWidth * scale);
        var physicalHeight = (int)Math.Ceiling(logicalHeight * scale);
        physicalHeight = Math.Clamp(physicalHeight, (int)(240 * scale), work.Height - (int)(48 * scale));
        window.Resize(new SizeInt32(physicalWidth, physicalHeight));
    }

    /// <summary>
    /// Absolute path to a bundled asset next to the executable. Using a relative path fails when the
    /// process is launched with a different working directory (e.g. the note editor started from the
    /// context menu, where the cwd is the clicked file's folder), so the window icon wouldn't load.
    /// </summary>
    public static string AssetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
}
