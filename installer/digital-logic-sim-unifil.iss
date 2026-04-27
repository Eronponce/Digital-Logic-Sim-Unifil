#define AppName "Digital Logic Sim Unifil"
#define AppPublisher "Unifil"
#define AppExeName "Digital-Logic-Sim-Unifil.exe"
#define AppVersion GetEnv("DLS_APP_VERSION")
#if AppVersion == ""
  #define AppVersion "2.1.6"
#endif
#define SourceDir GetEnv("DLS_BUILD_SOURCE_DIR")
#if SourceDir == ""
  #define SourceDir "..\Builds\Windows"
#endif

[Setup]
AppId={{6B292D3C-7C03-4D62-B3D6-6F2D7CE2713B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\Builds\Installer
OutputBaseFilename=DigitalLogicSim-Unifil-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
SetupLogging=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.zip,*.pdb,*.mdb,*.log"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
