; Inno Setup Script for Deskout
; Auto-generates a clean standalone Windows Setup Installer (EXE)

[Setup]
AppName=Deskout
AppVersion=3.0.0
DefaultDirName={autopf}\Deskout
DefaultGroupName=Deskout
UninstallDisplayIcon={app}\Deskout.exe
Compression=lzma2
SolidCompression=yes
OutputDir=publish_setup
OutputBaseFilename=DeskoutSetup
SetupIconFile=Assets\icon.ico
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
; Require Administrator privileges for Program Files installation
PrivilegesRequired=admin
CloseApplications=yes
AppMutex=Local\Deskout_Unique_Mutex_ID_10283

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\Deskout.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Deskout"; Filename: "{app}\Deskout.exe"
Name: "{autodesktop}\Deskout"; Filename: "{app}\Deskout.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Deskout"; ValueData: """{app}\Deskout.exe"" --background"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\Deskout.exe"; Parameters: "--background"; Description: "Launch Deskout in system tray"; Flags: nowait postinstall skipifsilent
