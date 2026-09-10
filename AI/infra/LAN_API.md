# Infra - LAN API

Dung khi task lien quan client/server LAN.

Files:
- `QuanLyHoSo.Server\Program.cs`
- `QuanLyHoSo.Server\ServerWindow.xaml`
- `QuanLyHoSo.Server\ServerWindow.xaml.cs`
- `QuanLyHoSo.Core\QuanLyHoSo.Core.csproj`
- `QuanLyHoSo.Shared\QuanLyHoSo.Shared.csproj`
- `Infrastructure\Configuration\AppPathSettings.cs`
- `Infrastructure\Network\LanApiModels.cs`
- `Infrastructure\Network\LanDataClient.cs`
- `Infrastructure\Network\LanDataServer.cs`
- `Infrastructure\Network\LanServerUnavailableException.cs`
- `Infrastructure\Data\AppDataService.cs`
- `scripts\test-lan-local.ps1`

Modes:
- `AdminHost`
- `Client`

Notes:
- Client khong mo SQLite, goi HTTP API toi admin host.
- Server API co the chay doc lap bang `QuanLyHoSo.Server`.
- App WPF mac dinh la `Client`; chi ghi ro `AdminHost` neu muon chay don may/tuong thich cu.
- Neu server tat/mat mang, client hien popup qua `LanServerUnavailableException`.
- `LanDataClient` gui header `X-QuanLyHoSo-Client` bang machine name va heartbeat route `health` moi 30 giay.
- `LanDataServer.ConnectedClientCount` dem machine duy nhat co heartbeat/request trong 90 giay gan nhat; stop server se clear danh sach.

Routes dang co:
- `auth/login`
- `catalog/areas`, `catalog/values`, `catalog/processors`
- `dashboard/metrics`, `dashboard/status`, `dashboard/areas`, `dashboard/trend`, `dashboard/recent`
- `records/list`, `records/count`, `records/export-preview`, `records/export-count`, `records/detail`, `records/similar`, `records/save`, `records/delete`, `records/total`
- `records/trash`, `records/trash/move`, `records/restore` (admin); `records/delete` la alias xoa mem va nhan them DeletionBatchId. Client moi goi trash/move de tranh xoa cung tren server cu.
- `records/trash/delete-permanently` (admin): RecordCode + DeletionBatchId; chi xoa ho so trong thung rac, tra bool.
- `processing/metrics`, `processing/list`, `processing/count`, `processing/detail`, `processing/update`
- `staff/performance`, `staff/deadlines`, `staff/active-records`
- `leadership-notices/latest`, `leadership-notices/save`
- `settings/catalog-items`, `settings/catalog-counts`, `settings/catalog/add`, `settings/catalog/update`, `settings/catalog/delete`, `settings/catalog/reorder`
- `settings/system-logs`, `settings/users`, `settings/users/save`, `settings/users/delete`, `settings/users/change-password`, `settings/backup/create`

Run:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

Lenh tren mo giao dien WPF quan tri server. Dong/thu nho se dua app xuong system tray; `Thoat may chu` moi stop listener. Day chua phai Windows Service.
