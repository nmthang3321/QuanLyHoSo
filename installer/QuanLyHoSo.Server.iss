#define MyAppName "QuanLyHoSo Server"
#define MyAppPublisher "QuanLyHoSo"
#define MyAppExeName "QuanLyHoSo.Server.exe"
#define ServerPort "5055"

[Setup]
AppId={{9E20A971-E090-4C76-BECE-0912F9CF2868}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\QuanLyHoSo Server
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=QuanLyHoSo-Server-Setup-{#AppVersion}-win-x64
SetupIconFile={#IconPath}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Tao bieu tuong ngoai man hinh"; GroupDescription: "Bieu tuong bo sung:"
Name: "startup"; Description: "Tu khoi dong Server khi dang nhap Windows"; GroupDescription: "Khoi dong:"

[Dirs]
Name: "{commonappdata}\QuanLyHoSo\Data"; Permissions: users-modify
Name: "{commonappdata}\QuanLyHoSo\Logs"; Permissions: users-modify

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--url http://0.0.0.0:{#ServerPort} --database ""{commonappdata}\QuanLyHoSo\Data\quanlyhoso.db"" --log-folder ""{commonappdata}\QuanLyHoSo\Logs"""
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--url http://0.0.0.0:{#ServerPort} --database ""{commonappdata}\QuanLyHoSo\Data\quanlyhoso.db"" --log-folder ""{commonappdata}\QuanLyHoSo\Logs"""; Tasks: desktopicon
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--url http://0.0.0.0:{#ServerPort} --database ""{commonappdata}\QuanLyHoSo\Data\quanlyhoso.db"" --log-folder ""{commonappdata}\QuanLyHoSo\Logs"""; Tasks: startup

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "http delete urlacl url=http://+:{#ServerPort}/"; Flags: runhidden waituntilterminated; StatusMsg: "Dang cap nhat quyen URL..."
Filename: "{sys}\netsh.exe"; Parameters: "http add urlacl url=http://+:{#ServerPort}/ sddl=""D:(A;;GX;;;BU)"""; Flags: runhidden waituntilterminated; StatusMsg: "Dang dang ky URL Server..."
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""QuanLyHoSo Server TCP {#ServerPort}"""; Flags: runhidden waituntilterminated; StatusMsg: "Dang cap nhat Firewall..."
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""QuanLyHoSo Server TCP {#ServerPort}"" dir=in action=allow protocol=TCP localport={#ServerPort} remoteip=localsubnet profile=any"; Flags: runhidden waituntilterminated; StatusMsg: "Dang mo cong Firewall {#ServerPort}..."
Filename: "{app}\{#MyAppExeName}"; Parameters: "--url http://0.0.0.0:{#ServerPort} --database ""{commonappdata}\QuanLyHoSo\Data\quanlyhoso.db"" --log-folder ""{commonappdata}\QuanLyHoSo\Logs"""; Description: "Mo {#MyAppName}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""QuanLyHoSo Server TCP {#ServerPort}"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveFirewallRule"
Filename: "{sys}\netsh.exe"; Parameters: "http delete urlacl url=http://+:{#ServerPort}/"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveUrlAcl"
