#define MyAppName "QuanLyHoSo Client"
#define MyAppPublisher "QuanLyHoSo"
#define MyAppExeName "QuanLyHoSo.exe"

[Setup]
AppId={{B9F22E35-70D0-4EA7-92CF-2B3A37B534E6}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\QuanLyHoSo
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=QuanLyHoSo-Client-Setup-{#AppVersion}-win-x64
SetupIconFile={#IconPath}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ServerPage: TInputQueryWizardPage;
  ServerUrl: string;

function NormalizeServerUrl(Value: string): string;
begin
  Result := Trim(Value);
  while (Length(Result) > 0) and (Result[Length(Result)] = '/') do
    Delete(Result, Length(Result), 1);
end;

function IsValidServerUrl(const Value: string): Boolean;
begin
  Result :=
    (CompareText(Copy(Value, 1, 7), 'http://') = 0) or
    (CompareText(Copy(Value, 1, 8), 'https://') = 0);
end;

function JsonEscape(Value: string): string;
begin
  Result := Value;
  StringChangeEx(Result, '\', '\\', True);
  StringChangeEx(Result, '"', '\"', True);
end;

function CanConnectToServer(const Value: string): Boolean;
var
  Request: Variant;
begin
  Result := False;
  try
    Request := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Request.SetTimeouts(3000, 3000, 3000, 3000);
    Request.Open('POST', Value + '/api/health', False);
    Request.SetRequestHeader('Content-Type', 'application/json');
    Request.SetRequestHeader('X-QuanLyHoSo-Version', '{#AppVersion}');
    Request.Send('{}');
    Result := (Request.Status = 200) and
      (Pos('"IsClientVersionSupported":true', Request.ResponseText) > 0);
  except
    Result := False;
  end;
end;

procedure InitializeWizard;
var
  ParameterUrl: string;
begin
  ServerPage := CreateInputQueryPage(
    wpSelectDir,
    'Server connection',
    'Enter the QuanLyHoSo server address',
    'Use the computer name (recommended) or a static IP address. Example: http://SERVER-PC:5055');
  ServerPage.Add('Server address:', False);

  ParameterUrl := ExpandConstant('{param:SERVERURL|http://SERVER-PC:5055}');
  ServerPage.Values[0] := NormalizeServerUrl(ParameterUrl);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID <> ServerPage.ID then
    Exit;

  ServerUrl := NormalizeServerUrl(ServerPage.Values[0]);
  if not IsValidServerUrl(ServerUrl) then
  begin
    MsgBox('The server address must begin with http:// or https://.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  ServerPage.Values[0] := ServerUrl;
  if not CanConnectToServer(ServerUrl) then
  begin
    Result := MsgBox(
      'Could not connect to the server at ' + ServerUrl + '.' + #13#10 + #13#10 +
      'Make sure the Server is running, both computers are on the same LAN, and port 5055 is open.' + #13#10 +
      'Do you want to continue the installation?',
      mbConfirmation,
      MB_YESNO) = IDYES;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsFolder: string;
  SettingsPath: string;
  SettingsJson: string;
begin
  if CurStep <> ssPostInstall then
    Exit;

  ServerUrl := NormalizeServerUrl(ServerPage.Values[0]);
  SettingsFolder := ExpandConstant('{localappdata}\QuanLyHoSo\Settings');
  SettingsPath := SettingsFolder + '\path-settings.json';
  ForceDirectories(SettingsFolder);

  SettingsJson :=
    '{' + #13#10 +
    '  "DataAccessMode": "Client",' + #13#10 +
    '  "AdminMachineName": "",' + #13#10 +
    '  "AdminServerUrl": "' + JsonEscape(ServerUrl) + '"' + #13#10 +
    '}' + #13#10;

  if not SaveStringToFile(SettingsPath, SettingsJson, False) then
    MsgBox('Could not save the connection settings to:' + #13#10 + SettingsPath, mbError, MB_OK);
end;
