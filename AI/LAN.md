# LAN prototype

Chi tiet hon: `AI/infra/LAN_API.md`.

Da co server console rieng: 1 may server giu DB va chay API LAN, client khong mo SQLite truc tiep. App WPF mac dinh la `Client`; `AdminHost` chi con la mode tuong thich/ky thuat neu can chay don may.

File chinh:
- `QuanLyHoSo.Server\Program.cs`
- `QuanLyHoSo.Server\QuanLyHoSo.Server.csproj`
- `QuanLyHoSo.Core\QuanLyHoSo.Core.csproj`
- `QuanLyHoSo.Shared\QuanLyHoSo.Shared.csproj`
- `Infrastructure\Configuration\AppPathSettings.cs`
- `Infrastructure\Network\LanApiModels.cs`
- `Infrastructure\Network\LanDataClient.cs`
- `Infrastructure\Network\LanDataServer.cs`
- `Infrastructure\Network\LanServerUnavailableException.cs`
- `Infrastructure\Data\AppDataService.cs`
- `scripts\test-lan-local.ps1`

Mode:
- `AdminHost`: mo SQLite local, seed/schema, dong thoi bat API noi bo LAN tai `AdminServerUrl`.
- `Client`: khong tao/mo SQLite, chi goi HTTP API toi may admin.
- Default config khi thieu `DataAccessMode` la `Client`. Muon chay WPF don may moi can ghi ro `AdminHost`.

Chay server console rieng:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

Luu y: `0.0.0.0` chi dung cho server listen moi card mang. Client khong ket noi bang `0.0.0.0`; client phai dung `http://localhost:5055` neu cung may hoac `http://IP-may-server:5055` neu may khac.

Tham so tuy chon:
- `--url http://0.0.0.0:5055`
- `--database C:\QuanLyHoSo\Data\quanlyhoso.db`
- `--log-folder C:\QuanLyHoSo\Logs`

Config mau server:

```json
{
  "DatabasePath": "C:\\QuanLyHoSo\\Data\\quanlyhoso.db",
  "LogFolder": "C:\\QuanLyHoSo\\Logs",
  "DataAccessMode": "AdminHost",
  "AdminMachineName": "MAY-ADMIN-01",
  "AdminServerUrl": "http://192.168.1.10:5055"
}
```

Config mau client:

```json
{
  "DatabasePath": "",
  "LogFolder": "C:\\QuanLyHoSo\\Logs",
  "DataAccessMode": "Client",
  "AdminMachineName": "MAY-ADMIN-01",
  "AdminServerUrl": "http://192.168.1.10:5055"
}
```

Neu server tat/mat mang, client hien popup qua `LanServerUnavailableException`: can bat app/server admin, cung LAN, firewall mo port 5055.

Test local:

```powershell
.\scripts\test-lan-local.ps1
```

Script se build, backup settings, mo 1 app `[SERVER]`, doi config sang `Client`, mo 1 app `[CLIENT]`, va restore settings khi dong app.

Da noi cac luong chinh:
- login
- dashboard/read stats
- danh sach ho so/filter/export preview
- xem chi tiet ho so
- nhap moi/sua form ho so qua server API cho admin
- queue xu ly
- cap nhat xu ly
- xoa ho so qua server
- settings: catalog counts/items/CRUD/reorder, system logs, user management
- backup DB server-side tu WPF admin qua route `settings/backup/create`

Chua lam day du:
- upload/copy file dinh kem tu client len server. Hien record save van luu `FilePath` text; neu file nam tren may client thi may server/may khac co the khong mo duoc path do.
- restore DB tu WPF client. Restore nen lam o server/bao tri de server doc duoc file backup.
- dong goi `QuanLyHoSo.Server` thanh Windows Service chay nen/start cung Windows.
- dong bo nhieu admin host. Khuyen nghi hien tai: 1 server giu DB, cac may WPF deu chay `Client`; admin dang nhap role `Admin`.
