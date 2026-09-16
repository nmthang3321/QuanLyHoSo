# Infrastructure - LAN API

Use for client/server LAN tasks.

Main files: server Program/Window/project, Core/Shared projects, `AppPathSettings`, LAN DTO/client/server/exception classes, `AppDataService`, and `scripts\test-lan-local.ps1`.

Modes: `AdminHost` and `Client`.

- Client never opens SQLite; it calls the server HTTP API.
- `QuanLyHoSo.Server` can host the API independently.
- WPF defaults to `Client`; use explicit `AdminHost` only for single-machine/legacy compatibility.
- Connection failures surface through `LanServerUnavailableException`.
- Client sends `X-QuanLyHoSo-Client` with the machine name and a 30-second `health` heartbeat.
- `ConnectedClientCount` counts unique machines active in the last 90 seconds and clears on server stop.

Route groups include authentication; catalogs; dashboard; record list/detail/save/resubmission/trash/restore; processing; staff performance/deadlines/active records; leadership notices/KPI; Settings catalog/log/user/password; backup/restore; and internal update discovery/download. Check `LanDataServer.Dispatch` for the exact current list.

Run:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

This opens the WPF server console. Close/minimize sends it to the system tray; only `Thoát máy chủ` stops it. It is not a Windows Service.
