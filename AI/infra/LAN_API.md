# Infra - LAN API

Dung khi task lien quan client/server LAN.

Files:
- `QuanLyHoSo.Server\Program.cs`
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
- App WPF van co AdminHost mode de tu chay DB/API neu can tuong thich cu.
- Neu server tat/mat mang, client hien popup qua `LanServerUnavailableException`.

Run:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```
