; Define a default if not provided
#ifndef MyAppVersion
#define MyAppVersion "0.0.0"
#pragma message "using default version"
#endif

#pragma message "Compiling version " + MyAppVersion

[Setup]
AppName=PlaylistRepo
AppVersion={#MyAppVersion}
AppPublisherURL=https://github.com/AidenBradley24/PlaylistRepoApp
AppSupportURL=https://github.com/AidenBradley24/PlaylistRepoApp/issues
AppUpdatesURL=https://github.com/AidenBradley24/PlaylistRepoApp/releases

DefaultDirName={autopf}\PlaylistRepo
DefaultGroupName=PlaylistRepo
OutputBaseFilename=PlaylistRepoInstaller
OutputDir=..\Output
Compression=lzma
SolidCompression=yes
DisableProgramGroupPage=yes

LicenseFile=..\LICENSE.txt
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=Aiden Bradley
VersionInfoDescription=PlaylistRepo CLI, API, and Web Interface
VersionInfoCopyright=© 2025 Aiden Bradley

[Files]
Source: "..\publish\PlaylistRepoCLI\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: ".\playlistrepo.bat"; DestDir: "{app}"

[Icons]
Name: "{group}\PlaylistRepo CLI"; Filename: "{app}\PlaylistRepoCLI.exe"

[Registry]
; Add PlaylistRepo to PATH (for current user)
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "PATH"; ValueData: "{olddata};{app}"; Flags: preservestringtype

[Code]
function IsYtDlpInstalled: Boolean;
var
  ResultCode: Integer;
begin
  { Try to detect yt-dlp.exe in PATH }
  Result := (Exec('cmd.exe', '/c where yt-dlp', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0));
end;

[Run]
; Run your app after install
Filename: "{app}\PlaylistRepoCLI.exe"; \
  Parameters: "serve"; \
  Description: "Run PlaylistRepo CLI"; \
  Flags: nowait postinstall skipifsilent
