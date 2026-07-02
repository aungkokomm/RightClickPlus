using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick.Infrastructure;

/// <summary>
/// Implements the Windows 11 classic-menu tweak by managing the well-known shell-extension
/// CLSID under <c>HKCU\Software\Classes\CLSID</c>. Presence of an empty <c>InprocServer32</c>
/// default value forces the classic context menu; removing the key restores the modern menu.
/// </summary>
public sealed class ContextMenuTweakService : IContextMenuTweakService
{
    // The undocumented-but-stable CLSID that, when blanked under HKCU, disables the
    // Windows 11 modern context menu and restores the classic one.
    private const string ClassicMenuClsid = "{86ca1aa0-34aa-4e8b-a509-50c905bafdc8}";
    private const string ClsidSubPath = $@"Software\Classes\CLSID\{ClassicMenuClsid}";
    private const string InprocSubKey = "InprocServer32";

    private readonly ILogger<ContextMenuTweakService> _logger;

    public ContextMenuTweakService(ILogger<ContextMenuTweakService> logger) => _logger = logger;

    public ContextMenuState GetState()
    {
        var isWin11 = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
        var classicForced = IsClassicTweakPresent();

        MenuMode mode = !isWin11
            ? MenuMode.NativeClassic
            : classicForced ? MenuMode.Classic : MenuMode.Modern;

        return new ContextMenuState
        {
            Mode = mode,
            IsWindows11 = isWin11,
            OsDescription = DescribeOs(isWin11),
        };
    }

    public async Task SetModeAsync(MenuMode mode, bool restartExplorer = true, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            _logger.LogInformation("SetModeAsync ignored: classic-menu tweak only applies to Windows 11.");
            return;
        }

        switch (mode)
        {
            case MenuMode.Classic:
                ForceClassic();
                break;
            case MenuMode.Modern:
                RestoreModern();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Only Classic or Modern are valid targets.");
        }

        if (restartExplorer)
        {
            await RestartExplorerAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RestartExplorerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting Explorer to apply context-menu change.");

        foreach (var proc in Process.GetProcessesByName("explorer"))
        {
            try
            {
                proc.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill an explorer process (PID {Pid}).", proc.Id);
            }
            finally
            {
                proc.Dispose();
            }
        }

        // Give the shell a moment, then ensure it comes back (Windows usually auto-restarts it).
        await Task.Delay(600, cancellationToken).ConfigureAwait(false);

        if (Process.GetProcessesByName("explorer").Length == 0)
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
    }

    private static bool IsClassicTweakPresent()
    {
        using var clsid = Registry.CurrentUser.OpenSubKey(ClsidSubPath);
        using var inproc = clsid?.OpenSubKey(InprocSubKey);
        if (inproc is null)
        {
            return false;
        }

        // The tweak is "present" when the InprocServer32 default value is empty.
        var value = inproc.GetValue(null) as string;
        return string.IsNullOrEmpty(value);
    }

    private void ForceClassic()
    {
        using var clsid = Registry.CurrentUser.CreateSubKey(ClsidSubPath, writable: true);
        using var inproc = clsid.CreateSubKey(InprocSubKey, writable: true);
        inproc.SetValue(null, string.Empty, RegistryValueKind.String);
        _logger.LogInformation("Classic context menu forced (CLSID tweak written under HKCU).");
    }

    private void RestoreModern()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(ClsidSubPath, throwOnMissingSubKey: false);
            _logger.LogInformation("Modern context menu restored (CLSID tweak removed).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove the classic-menu CLSID tweak.");
            throw;
        }
    }

    private static string DescribeOs(bool isWin11)
    {
        var v = Environment.OSVersion.Version;
        var name = isWin11 ? "Windows 11" : "Windows 10";
        return $"{name} (build {v.Build})";
    }
}
