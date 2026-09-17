# LAN client/server architecture

Detailed reference: `engineering/infra/LAN_API.md`.

A separate WPF server owns SQLite and hosts the LAN API; clients do not open SQLite directly. The WPF application defaults to `Client`. `AdminHost` remains only for single-machine/technical compatibility.

Main files: the Server project, Core/Shared projects, `AppPathSettings`, LAN DTO/client/server/exception classes, `AppDataService`, and `scripts\test-lan-local.ps1`.

Modes:
- `AdminHost`: opens local SQLite, runs schema/seed, and hosts the API at `AdminServerUrl`.
- `Client`: never opens SQLite; calls the server HTTP API.
- Missing/unknown `DataAccessMode` normalizes to `Client`.

Run the server UI:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

`0.0.0.0` is a listen address only. Clients use `http://localhost:5055` on the same machine or the server IP/host name from another machine.

Optional arguments: `--url`, `--database`, `--log-folder`, and `--sample-data`.

Example server settings:

```json
{
  "DatabasePath": "C:\\QuanLyHoSo\\Data\\quanlyhoso.db",
  "LogFolder": "C:\\QuanLyHoSo\\Logs",
  "DataAccessMode": "AdminHost",
  "AdminMachineName": "MAY-ADMIN-01",
  "AdminServerUrl": "http://192.168.1.10:5055"
}
```

Example client settings use `DataAccessMode: "Client"` and a reachable server URL. When the server is unavailable, `LanServerUnavailableException` produces a user-facing prompt to start it, verify LAN connectivity, and open firewall port 5055.

Local test: `.\scripts\test-lan-local.ps1`. It builds, backs up settings, starts server/client instances, switches client configuration, and restores settings afterward.

Connected flows include authentication, dashboards, record list/filter/export/details, Admin intake/edit/delete, processing queue/update, Settings catalog/log/users, backup/restore, resubmission, trash, staff tracking, notices/KPI, and internal update packages.

Known limitations:
- Attachment metadata/path travels through LAN, but physical client files are not uploaded to server storage.
- Server is a tray application tied to a user session, not a Windows Service.
- Use one server owning the database; do not run multiple synchronized Admin hosts.

Server UI:
- Compact status surface shows one usable client URL, machine name, uptime, and connected client count, without database/log internals.
- Green status dot pulses while listening.
- Client heartbeat is every 30 seconds; unique machines active within 90 seconds are counted.
- Controls support server start/stop, built-in Admin reset, opening data/log folders, and copying the URL.
- Closing/minimizing hides to tray; only explicit exit stops the server.
