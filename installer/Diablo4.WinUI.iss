#ifndef AppVersion
#define AppVersion "1.0.0.0"
#endif

#ifndef RepoRoot
#define RepoRoot AddBackslash(SourcePath + "..")
#endif

#ifndef PublishDir
#define PublishDir AddBackslash(RepoRoot + "artifacts\\publish\\win-x64")
#endif

#define AppName "Kontrola pařby"
#define AppPublisher "Petr Jukl"
#define AppExeName "Diablo4.WinUI.exe"

[Setup]
AppId={{A82D56BB-A8C6-4D8F-8B28-D0A8A7D9250E}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/PetrJukl/Diablo_4
AppSupportURL=https://github.com/PetrJukl/Diablo_4/issues
AppUpdatesURL=https://github.com/PetrJukl/Diablo_4/releases
DefaultDirName={localappdata}\Programs\Kontrola parby
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#RepoRoot}Diablo4.WinUI\Assets\211668_controller_b_game_icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir={#RepoRoot}artifacts\installer
OutputBaseFilename=KontrolaParbySetup-{#AppVersion}
CloseApplications=yes
CloseApplicationsFilter={#AppExeName}
RestartApplications=no
UsePreviousAppDir=yes
ChangesAssociations=no

[Languages]
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"

[Tasks]
Name: "desktopicon"; Description: "Vytvořit ikonu na ploše"; Flags: unchecked

[Files]
Source: "{#PublishDir}*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Spustit {#AppName}"; Flags: nowait postinstall skipifsilent
