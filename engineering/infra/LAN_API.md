# Infrastructure - LAN API

Use for client/server LAN tasks.

Main files: server Program/Window/project, Core/Shared projects, `AppPathSettings`, LAN DTO/client/server/exception classes, `AppDataService`, and `scripts\test-lan-local.ps1`.

Modes: `AdminHost` and `Client`.

- Client never opens SQLite; it calls the server HTTP API.
- `QuanLyHoSo.Server` can host the API independently.
- WPF defaults to `Client`; use explicit `AdminHost` only for single-machine/legacy compatibility.
- Connection failures surface through `LanServerUnavailableException`.
- Client sends `X-QuanLyHoSo-Client` with the machine name and `X-QuanLyHoSo-Client-Version` with its actual three-part product version for diagnostics. `X-QuanLyHoSo-Version` remains as a legacy compatibility header.
- `health` returns the actual `ServerVersion`. The legacy `RequiredClientVersion` and `IsClientVersionSupported` fields remain in the response for compatibility, but no product-version equality is required.
- The 30-second heartbeat checks only that the server reports a ready health state. Business routes are not rejected because the Client and Server product versions differ.
- When a pre-change Server still reports an unsupported Client version, the updated Client automatically uses that Server's requested value only in the legacy compatibility header. Its actual version remains available in `X-QuanLyHoSo-Client-Version`, so a Client-only rollout can connect to an older Server without upgrading the Server first.
- `ConnectedClientCount` counts unique machines active in the last 90 seconds and clears on server stop.

Route groups include authentication; catalogs; dashboard; record list/detail/save/resubmission/trash/restore; attachment upload metadata and authenticated `attachments/download`; processing; staff performance/deadlines/active records; leadership notices/KPI; Settings catalog/log/user/password; backup/restore; and internal update discovery/download/start. Client packages use `QuanLyHoSo-Client-*.zip`; Server packages use `QuanLyHoSo-Server-*.zip`. Only an authenticated Admin session can discover a Server package and invoke the Server update route. Older Servers without the overview route still support the Client-only discovery fallback. Check `LanDataServer.Dispatch` for the exact current list.

File responses use an ASCII-safe `Content-Disposition` fallback plus RFC 5987 `filename*=UTF-8''...`. Do not place Vietnamese or other non-ASCII filenames directly in an `HttpListener` header; Windows rejects those header values before streaming the file.

New attachment drafts serialize their bytes in `AttachmentDraft.Content`. The server writes the durable copy under `QuanLyHoSoFiles\Attachments` and returns server-owned metadata. Backup creation produces `.qlhbackup` packages containing SQLite plus all managed files; restore still accepts legacy database-only `.db` files.

Run:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

This opens the WPF server console. Close/minimize sends it to the system tray; only `Thoát máy chủ` stops it. It is not a Windows Service.
