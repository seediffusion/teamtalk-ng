; TeamTalk NG Windows installer.
;
; Build a publish folder first, then compile this script with Inno Setup:
;   dotnet publish src\TeamTalkNg.App\TeamTalkNg.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish\TeamTalkNG
;   iscc installer\windows\TeamTalkNg.iss
;
; To include BearWare's TeamTalk runtime in a trusted prerelease build, place
; TeamTalk5.dll in the publish folder before compiling the installer.

#define AppName "TeamTalk NG"
#define AppPublisher "Seediffusion"
#define AppVersion "0.1.0"
#define AppFileVersion "0.1.0.0"
#define AppExeName "TeamTalkNg.App.exe"

#ifndef PublishDir
  #define PublishDir "..\..\artifacts\publish\TeamTalkNG"
#endif

#define AppExePath PublishDir + "\" + AppExeName

#if !FileExists(AppExePath)
  #error "Publish output was not found. Run dotnet publish first, or compile with /DPublishDir=<path>."
#endif

[Setup]
AppId={{7F874F32-DB15-48D6-AF5C-B4D5E54B9E50}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppFileVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\TeamTalk NG
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir=..\..\artifacts\installer
OutputBaseFilename=TeamTalkNG_v{#AppVersion}_Setup
Compression=lzma2/ultra64
SolidCompression=yes
LicenseFile=..\..\LICENSE
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
ChangesAssociations=yes
WizardStyle=modern

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "associateurl"; Description: "Register tt:// TeamTalk links for TeamTalk NG"; GroupDescription: "Connection shortcuts:"; Flags: checkedonce
Name: "associatefiles"; Description: "Register .tt connection files for TeamTalk NG"; GroupDescription: "Connection shortcuts:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; WorkingDir: "{app}"; Flags: postinstall nowait skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Classes\tt"; ValueType: string; ValueData: "URL:TeamTalk Protocol"; Flags: uninsdeletekey; Tasks: associateurl
Root: HKCU; Subkey: "Software\Classes\tt"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletekey; Tasks: associateurl
Root: HKCU; Subkey: "Software\Classes\tt\DefaultIcon"; ValueType: string; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletekey; Tasks: associateurl
Root: HKCU; Subkey: "Software\Classes\tt\Shell"; ValueType: string; ValueData: "Open"; Flags: uninsdeletekey; Tasks: associateurl
Root: HKCU; Subkey: "Software\Classes\tt\Shell\Open\Command"; ValueType: string; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associateurl

Root: HKCU; Subkey: "Software\Classes\.tt"; ValueType: string; ValueData: "TeamTalkNg.ConnectionTarget"; Flags: uninsdeletekey; Tasks: associatefiles
Root: HKCU; Subkey: "Software\Classes\TeamTalkNg.ConnectionTarget"; ValueType: string; ValueData: "TeamTalk connection target"; Flags: uninsdeletekey; Tasks: associatefiles
Root: HKCU; Subkey: "Software\Classes\TeamTalkNg.ConnectionTarget\DefaultIcon"; ValueType: string; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletekey; Tasks: associatefiles
Root: HKCU; Subkey: "Software\Classes\TeamTalkNg.ConnectionTarget\Shell"; ValueType: string; ValueData: "Open"; Flags: uninsdeletekey; Tasks: associatefiles
Root: HKCU; Subkey: "Software\Classes\TeamTalkNg.ConnectionTarget\Shell\Open\Command"; ValueType: string; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: associatefiles
