# Infra - LAN API

Dung khi task lien quan client/server LAN.

Files:
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
- Server API dang nhung trong app admin, chua tach Windows service.
- Neu server tat/mat mang, client hien popup qua `LanServerUnavailableException`.

