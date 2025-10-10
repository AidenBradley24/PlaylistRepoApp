#define MyAppVersion GetStringParam("MyAppVersion", "0.0.0")

[Setup]
AppName=PlaylistRepo
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\PlaylistRepo
DefaultGroupName=PlaylistRepo
OutputBaseFilename=PlaylistRepoInstaller
OutputDir=Output
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\PlaylistRepoCLI\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\PlaylistRepo CLI"; Filename: "{app}\PlaylistRepoCLI.exe"

[Registry]
; Add PlaylistRepo to PATH (if not already present)
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "PATH"; ValueData: "{olddata};{app}"; Flags: preservestringtype

[Run]
; Launch CLI after install (optional)
Filename: "{app}\PlaylistRepoCLI.exe"; Description: "Run PlaylistRepo CLI"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    ; Tell Windows to update environment variables for new PATH
    SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, LPARAM('Environment'), SMTO_ABORTIFHUNG, 5000, 0);
  end;
end;
