<#
  Rebuilds docs/banner.png, the banner used by the README, the portfolio page and the release notes.

    .\make-banner.ps1             # re-render with the current version and the saved window capture
    .\make-banner.ps1 -Capture    # first re-capture the main window from the installed app

  - The version pill comes from <Version> in Directory.Build.props.
  - The icons are taken from the app's own Assets, so the banner always matches the menu.
  - -Capture opens the installed app, switches it to Light theme for a moment, captures its window
    into window.png, closes it, and then restores the settings file exactly (content and timestamp).
  - Rendering uses Microsoft Edge in headless mode at the banner's size, 1774 x 887.
#>
param(
    [switch]$Capture,
    [string]$AppPath = 'C:\Program Files\RightClickPlus\RightClick.App.exe'
)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$out  = Join-Path $root 'docs\banner.png'
Add-Type -AssemblyName System.Drawing

function Export-Icons {
    # Saves the largest frame of each app icon as PNG (PNG frames are copied byte for byte).
    $dir = Join-Path $here 'icons'
    New-Item -ItemType Directory -Force $dir | Out-Null
    foreach ($ico in Get-ChildItem -LiteralPath (Join-Path $root 'src\RightClick.App\Assets') -Filter *.ico) {
        $b = [IO.File]::ReadAllBytes($ico.FullName)
        $best = $null
        for ($i = 0; $i -lt [BitConverter]::ToUInt16($b, 4); $i++) {
            $o = 6 + 16 * $i
            $w = if ($b[$o] -eq 0) { 256 } else { $b[$o] }
            if (-not $best -or $w -gt $best.W) { $best = @{ W = $w; Len = [BitConverter]::ToInt32($b, $o + 8); Off = [BitConverter]::ToInt32($b, $o + 12) } }
        }
        $png = Join-Path $dir ($ico.BaseName + '.png')
        if ($b[$best.Off] -eq 0x89 -and $b[$best.Off + 1] -eq 0x50) {
            [IO.File]::WriteAllBytes($png, $b[$best.Off..($best.Off + $best.Len - 1)])
        }
        else {
            $icon = New-Object Drawing.Icon($ico.FullName, $best.W, $best.W)
            $icon.ToBitmap().Save($png, [Drawing.Imaging.ImageFormat]::Png)
            $icon.Dispose()
        }
    }
}

function Save-WindowCapture([string]$target) {
    if (-not (Test-Path -LiteralPath $AppPath)) { throw "RightClick+ is not installed at $AppPath (use -AppPath)." }
    if (-not ('RcpBannerWin' -as [type])) {
        Add-Type -TypeDefinition @'
using System; using System.Runtime.InteropServices;
public static class RcpBannerWin {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
  [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int s);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool attach);
  [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
  // Best effort: Windows only lets the foreground thread hand out focus, so borrow its input state.
  public static void Activate(IntPtr h) {
    uint fg = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero), me = GetCurrentThreadId();
    AttachThreadInput(fg, me, true); BringWindowToTop(h); SetForegroundWindow(h); AttachThreadInput(fg, me, false);
  }
}
'@
    }
    [void][RcpBannerWin]::SetProcessDpiAwarenessContext([IntPtr](-4))   # per-monitor v2: physical pixels

    $settings  = Join-Path $env:LOCALAPPDATA 'RightClickPlus\settings.json'
    $hadFile   = Test-Path -LiteralPath $settings
    $origBytes = if ($hadFile) { [IO.File]::ReadAllBytes($settings) } else { $null }
    $origTime  = if ($hadFile) { (Get-Item -LiteralPath $settings).LastWriteTimeUtc } else { $null }
    $p = $null
    try {
        $json = if ($hadFile) { [Text.Encoding]::UTF8.GetString($origBytes) -replace '"Theme":\s*\d+', '"Theme": 1' } else { '{ "Theme": 1 }' }
        New-Item -ItemType Directory -Force (Split-Path $settings) | Out-Null
        [IO.File]::WriteAllText($settings, $json)                         # 1 = Light

        $p = Start-Process -FilePath $AppPath -PassThru
        $deadline = (Get-Date).AddSeconds(20)
        while ($p.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 200; $p.Refresh() }
        $h = $p.MainWindowHandle
        if ($h -eq [IntPtr]::Zero) { throw 'The RightClick+ window did not appear.' }
        Start-Sleep -Seconds 3                                            # let it lay out and draw Mica

        # The window is a pixel or two shorter than its content, so activating it flashes a scroll
        # indicator. Give it 6 px of room (invisible at banner size) so there is nothing to flash.
        $o = New-Object RcpBannerWin+RECT; [void][RcpBannerWin]::GetWindowRect($h, [ref]$o)
        [void][RcpBannerWin]::SetWindowPos($h, [IntPtr](-1), 80, 60, $o.R - $o.L, $o.B - $o.T + 6, 0x0040)   # topmost
        Start-Sleep -Milliseconds 500
        [RcpBannerWin]::Activate($h)
        Start-Sleep -Milliseconds 1500

        $r = New-Object RcpBannerWin+RECT
        [void][RcpBannerWin]::DwmGetWindowAttribute($h, 9, [ref]$r, 16)   # visible frame, without the shadow
        $bmp = New-Object Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
        $g = [Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size); $g.Dispose()
        $bmp.Save($target, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
        [void][RcpBannerWin]::SetWindowPos($h, [IntPtr](-2), 0, 0, 0, 0, 0x0003)                # not topmost again
        Write-Host "Captured the window into $target"
    }
    finally {
        if ($p -and -not $p.HasExited) { [void]$p.CloseMainWindow(); if (-not $p.WaitForExit(5000)) { $p.Kill() } }
        if ($hadFile) {
            [IO.File]::WriteAllBytes($settings, $origBytes)
            (Get-Item -LiteralPath $settings).LastWriteTimeUtc = $origTime
        }
        else {
            Remove-Item -LiteralPath $settings -ErrorAction SilentlyContinue  # it didn't exist before
        }
    }
}

function Invoke-Render {
    $edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $edge) { throw 'Microsoft Edge was not found.' }

    $version = (Select-Xml -Path (Join-Path $root 'Directory.Build.props') -XPath '//Version').Node.InnerText
    $page = Join-Path $here '_render.html'                                # next to the template, for its relative paths
    $tmp  = Join-Path ([IO.Path]::GetTempPath()) "rightclickplus-banner-$PID"
    New-Item -ItemType Directory -Force $tmp | Out-Null
    $shot = Join-Path $tmp 'banner.png'
    try {
        [IO.File]::WriteAllText($page, [IO.File]::ReadAllText((Join-Path $here 'banner.html')).Replace('{{VERSION}}', $version))
        $edgeArgs = '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1', '--window-size=1774,887',
                    "--screenshot=`"$shot`"", "--user-data-dir=`"$(Join-Path $tmp 'profile')`"", ([Uri]$page).AbsoluteUri
        Start-Process -FilePath $edge -ArgumentList $edgeArgs -WindowStyle Hidden
        # msedge.exe can return before the screenshot is written; wait for it.
        $deadline = (Get-Date).AddSeconds(30)
        while (-not (Test-Path -LiteralPath $shot) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250 }
        if (-not (Test-Path -LiteralPath $shot)) { throw 'Edge did not produce the screenshot.' }
        Start-Sleep -Milliseconds 500
        Copy-Item -LiteralPath $shot -Destination $out -Force
        Write-Host "Rendered $out for v$version"
    }
    finally {
        Remove-Item -LiteralPath $page -ErrorAction SilentlyContinue
        # Edge keeps its profile folder open for a moment while it shuts down.
        for ($i = 0; $i -lt 20 -and (Test-Path -LiteralPath $tmp); $i++) {
            Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
            if (Test-Path -LiteralPath $tmp) { Start-Sleep -Milliseconds 500 }
        }
    }
}

Export-Icons
if ($Capture) { Save-WindowCapture (Join-Path $here 'window.png') }
Invoke-Render
