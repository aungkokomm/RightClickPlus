; Inno Setup script for RightClick+ — portable WinUI 3 context-menu manager (unpackaged, self-contained).
; Built by build.ps1, which publishes the app then invokes:
;   ISCC.exe /DAppVersion=x.y.z /DSrcDir="<publish dir>" installer\RightClickPlus.iss

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef SrcDir
  #define SrcDir "..\src\RightClick.App\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish"
#endif

#define AppName    "RightClick+"
#define Publisher  "Aung Ko Ko"
#define ExeName    "RightClick.App.exe"

[Setup]
AppId={{B7E4D9A2-1F3C-4A8E-9C6D-RIGHTCLICK001}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={autopf}\RightClickPlus
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Per-user install so no admin/UAC prompt is needed.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=RightClickPlus-Setup-{#AppVersion}
SetupIconFile=..\src\RightClick.App\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#ExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
; The entire self-contained publish output (app + WinAppSDK + .NET runtime).
Source: "{#SrcDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#ExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove the per-user shell verbs (HKCU) BEFORE the files are deleted, so no orphaned, broken
; right-click entries remain pointing at the removed CLI. Runs while RightClick.Cli.exe still exists.
Filename: "{app}\RightClick.Cli.exe"; Parameters: "unregister-all"; Flags: runhidden; RunOnceId: "RemoveShellVerbs"

[UninstallDelete]
; Remove per-user state the app writes (settings, logs).
Type: filesandordirs; Name: "{localappdata}\RightClickPlus"
