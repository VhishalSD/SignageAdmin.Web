[Setup]
AppName=Signage Beheer
AppVersion=1.0.0
AppPublisher=Vishal Tewari
DefaultDirName={localappdata}\Programs\Signage Beheer
DefaultGroupName=Signage Beheer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=dist
OutputBaseFilename=Signage Beheer Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Properties\AppIcon.ico
UninstallDisplayIcon={app}\Signage Beheer.exe

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"

[Tasks]
Name: "desktopicon"; Description: "Maak een snelkoppeling op het bureaublad"; GroupDescription: "Extra opties:"; Flags: unchecked

[Files]
Source: "dist\Signage Beheer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Signage Beheer"; Filename: "{app}\Signage Beheer.exe"
Name: "{autodesktop}\Signage Beheer"; Filename: "{app}\Signage Beheer.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Signage Beheer.exe"; Description: "Start Signage Beheer"; Flags: nowait postinstall skipifsilent
