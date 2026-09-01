# LAN prototype

Chi tiet hon: `AI/infra/LAN_API.md`.

Da lam ban don gian: 1 may server/admin giu DB, client khong mo SQLite truc tiep.

File chinh:
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
- queue xu ly
- cap nhat xu ly
- xoa ho so qua server

Chua lam day du:
- nhap moi/sua form ho so tu client qua API, dac biet attachment/upload file.
- service Windows chay nen rieng; hien server API dang nhung trong app admin.
- dong bo nhieu admin host. Khuyen nghi hien tai: 1 admin host giu DB, admin phu chay `Client` nhung dang nhap role `Admin`.
