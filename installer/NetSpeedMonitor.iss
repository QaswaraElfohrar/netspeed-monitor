; Inno Setup 6 script - built by build.ps1 (passes AppVersion and SourceExe)
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceExe
  #define SourceExe "..\dist\NetSpeedMonitor\NetSpeedMonitor.exe"
#endif

[Setup]
AppId={{6B0E5C1E-7D6A-4C0B-9A57-2F3C8E6B1A41}
AppName=NetSpeed Monitor
AppVersion={#AppVersion}
AppPublisher=Qaswara Elfohrar Programming for Information Technology
AppPublisherURL=https://qaswaraelfohrar.com
AppSupportURL=https://www.facebook.com/QaswaraElfohrar
DefaultDirName={autopf}\NetSpeed Monitor
DefaultGroupName=NetSpeed Monitor
DisableProgramGroupPage=yes
OutputBaseFilename=NetSpeedMonitor-{#AppVersion}-setup
SetupIconFile=..\src\NetSpeedMonitor.App\Assets\app.ico
UninstallDisplayIcon={app}\NetSpeedMonitor.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "startup"; Description: "Start NetSpeed Monitor with Windows"; GroupDescription: "Options:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Options:"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\NetSpeed Monitor"; Filename: "{app}\NetSpeedMonitor.exe"
Name: "{autodesktop}\NetSpeed Monitor"; Filename: "{app}\NetSpeedMonitor.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NetSpeedMonitor"; ValueData: """{app}\NetSpeedMonitor.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\NetSpeedMonitor.exe"; Description: "Launch NetSpeed Monitor"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /IM NetSpeedMonitor.exe /F"; Flags: runhidden; RunOnceId: "KillApp"
