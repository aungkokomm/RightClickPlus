<#
.SYNOPSIS
  Build, publish (portable self-contained), and package RightClick+ into an Inno Setup installer.

.DESCRIPTION
  Single source of version truth is Directory.Build.props. Every build can bump the
  patch component (-BumpPatch). The app is published unpackaged + self-contained so it
  is fully portable (no .NET or WinAppSDK install required), then ISCC wraps it.

.EXAMPLE
  .\build.ps1 -BumpPatch          # bump patch, build, publish, make installer
  .\build.ps1 -SkipInstaller      # build + publish only
  .\build.ps1 -Configuration Debug -SkipPublish  # quick compile check
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$BumpPatch,
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$tfm = "net10.0-windows10.0.26100.0"
$rid = "win-$Platform"
$appProj = Join-Path $root "src\RightClick.App\RightClick.App.csproj"
$cliProj = Join-Path $root "src\RightClick.Cli\RightClick.Cli.csproj"
$propsFile = Join-Path $root "Directory.Build.props"

function Find-ISCC {
    foreach ($p in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $p) { return $p }
    }
    throw "ISCC.exe (Inno Setup 6) not found."
}

# --- version ---
[xml]$props = Get-Content $propsFile
$verNode = $props.Project.PropertyGroup.Version
if ($BumpPatch) {
    $parts = $verNode.Split('.')
    $parts[2] = [int]$parts[2] + 1
    $verNode = ($parts -join '.')
    $props.Project.PropertyGroup.Version = $verNode
    $props.Save($propsFile)
    Write-Host "Version bumped to $verNode" -ForegroundColor Cyan
}
$version = $verNode
Write-Host "RightClick+ build  version=$version  config=$Configuration  platform=$Platform" -ForegroundColor Green

# The .NET 10 SDK (dotnet) builds WinUI 3 correctly here, including the XAML/PRI toolchain.
if ($SkipPublish) {
    dotnet build $appProj -c $Configuration -p:Platform=$Platform --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    Write-Host "Build succeeded." -ForegroundColor Green
    return
}

# --- publish (portable, self-contained) ---
$publishDir = Join-Path $root "src\RightClick.App\bin\$Configuration\$tfm\$rid\publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish $appProj -c $Configuration -p:Platform=$Platform -r $rid `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishReadyToRun=true -p:PublishTrimmed=false `
    --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }
Write-Host "Published app to $publishDir" -ForegroundColor Green

# --- publish CLI helper into the SAME folder (reuses the app's self-contained runtime) ---
dotnet publish $cliProj -c $Configuration -p:Platform=$Platform -r $rid `
    --self-contained true `
    -p:PublishReadyToRun=true -p:PublishTrimmed=false `
    -o $publishDir --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed." }
Write-Host "Published CLI helper alongside the app." -ForegroundColor Green

if ($SkipInstaller) { return }

# --- installer ---
$iscc = Find-ISCC
$iss = Join-Path $root "installer\RightClickPlus.iss"
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null

& $iscc "/DAppVersion=$version" "/DSrcDir=$publishDir" $iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed." }
Write-Host "Installer created: dist\RightClickPlus-Setup-$version.exe" -ForegroundColor Green
