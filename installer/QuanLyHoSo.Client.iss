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

[Tasks]
Name: "desktopicon"; Description: "Tao bieu tuong ngoai man hinh"; GroupDescription: "Bieu tuong bo sung:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Mo {#MyAppName}"; Flags: nowait postinstall skipifsilent

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
    Request.Send('{}');
    Result := Request.Status = 200;
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
    'Ket noi may chu',
    'Nhap dia chi may chu QuanLyHoSo',
    'Dung ten may (khuyen nghi) hoac IP tinh. Vi du: http://SERVER-PC:5055');
  ServerPage.Add('Dia chi may chu:', False);

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
    MsgBox('Dia chi may chu phai bat dau bang http:// hoac https://.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  ServerPage.Values[0] := ServerUrl;
  if not CanConnectToServer(ServerUrl) then
  begin
    Result := MsgBox(
      'Chua ket noi duoc may chu tai ' + ServerUrl + '.' + #13#10 + #13#10 +
      'Hay kiem tra Server dang chay, hai may cung mang LAN va cong 5055 da duoc mo.' + #13#10 +
      'Ban van muon tiep tuc cai dat?',
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
    MsgBox('Khong the luu cau hinh ket noi tai:' + #13#10 + SettingsPath, mbError, MB_OK);
end;
