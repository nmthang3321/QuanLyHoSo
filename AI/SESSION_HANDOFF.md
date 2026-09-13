# Session handoff - QuanLyHoSo

Cap nhat: 2026-09-13

File nay nam trong `AI/` de gom toan bo context cho AI vao mot cho. Khi bat dau analyse:

1. Doc `AI/INDEX.md`.
2. Chay `git status --short --branch`.
3. Chi doc file trong `AI/` dung voi trang/chuc nang dang lam.

Routing chinh:
- Routing day du: `AI/INDEX.md`
- Trang nho: `AI/pages/*.md`
- Popup/overlay: `AI/popups/*.md`
- Chuc nang dung chung: `AI/features/*.md`
- Infra/DB/build/LAN: `AI/infra/*.md`
- Automated tests: `AI/infra/TESTING.md`

## Snapshot 2026-09-13 - Automated regression tests

- Da them 3 test projects vao solution, tach biet trong `tests/`: UnitTests, IntegrationTests, UITests. Framework: xUnit, Moq, Coverlet, FlaUI UIA3; test projects target .NET 8, app target .NET 5 khong doi.
- Ket qua da verify ngay 2026-09-12: Release solution build pass; 36 unit/ViewModel + 20 SQLite integration + 1 WPF UI smoke = 57 test cases pass. Lenh full da verify: `tests/Scripts/run-all.ps1 -IncludeUI`. Day la ket qua lich su, khong thay cho viec chay lai sau thay doi.
- Tests bao ve password/auth/role, commands/notifications, forced password-change validation, area selector, CRUD/reopen/Unicode/attachments metadata, search/pagination, rollback khi trung ma, trash/stale undo/permanent delete, backup/restore/corrupt backup.
- Production testability changes: internal `AppDataService(string databasePath)` khong tao LAN listener; `InternalsVisibleTo` cho IntegrationTests; path override `QUANLYHOSO_TEST_ROOT` chi khi env duoc set; 4 AutomationIds login; root csproj exclude `tests/**` khoi Compile. Khong doi UI visual hay business rules.
- Moi integration test tao DB rieng trong OS temp va cleanup; UI smoke launch child process voi profile rieng. Generated reports ignored trong git.
- Chua cover full processing transitions, export contents, LAN failures/concurrency, document generation, file locks/permission denial va authenticated UI workflows. Khong coi bo test hien tai la full feature coverage.
- Huong dan chay/duyet test: `AI/infra/TESTING.md`, `tests/README.md`; inventory: `tests/TEST_MATRIX.md`.

## Snapshot 2026-09-03

- Repo da tach `QuanLyHoSo.Shared`, `QuanLyHoSo.Core`, `QuanLyHoSo.Server`.
- `QuanLyHoSo.Server` la console server giu SQLite DB/log/API. Chay bang:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

- WPF `QuanLyHoSo` mac dinh la `Client`. Neu `DataAccessMode` thieu/lạ thi normalize ve `Client`; chi `AdminHost` ro rang moi chay local DB.
- Trang Settings WPF khong con popup cai dat DB/log/url. Cac thong so do thuoc server.
- Settings van co catalog, user management, system logs, update software va backup.
- Backup trong Settings la server-side: admin bam Sao luu ngay -> client goi `settings/backup/create` -> server tao file trong `%LocalAppData%\QuanLyHoSo\Backup` tren may server.
- Admin trong WPF client van co Nhap du lieu va luu/sua/xoa ho so qua LAN API (`records/similar`, `records/save`, `records/delete`).
- Nut sua ho so o danh sach duoc mo theo `AuthContext.CanEditRecord(record.ProcessorName)`, khong khoa theo client mode.
- Officer/can bo khong thay muc Nhap du lieu, khong them/xoa ho so; chi xem/chinh sua/phan loai theo quyen va khong duoc lui workflow ve buoc truoc phan cong.
- Attachment LAN hien moi luu metadata/path; chua co upload/copy file vat ly tu client len server.

## Snapshot 2026-09-09

- `QuanLyHoSo.Server` da co giao dien WPF quan tri co ban thay cho cua so console.
- Giao dien server compact, chi hien mot URL ket noi client dung duoc, ten may, uptime va so client dang ket noi; khong hien URL lang nghe/duong dan DB/noi dung log ky thuat.
- Cham xanh trang thai server pulse cham khi listener dang chay.
- Client gui heartbeat 30 giay kem header `X-QuanLyHoSo-Client`; server dem ten may duy nhat co request/heartbeat trong 90 giay gan nhat.
- Admin tai may server co the start/stop LAN server, reset built-in `admin`, mo thu muc data/log va copy URL cho client. Sao luu du lieu thuc hien trong Settings cua app WPF sau khi Admin dang nhap.
- Reset Admin tren server tao mat khau tam 8 ky tu cho username `admin`, co nut copy va bat buoc doi mat khau khi dang nhap.
- User moi hoac user duoc Admin cap lai mat khau deu phai doi mat khau tren man hinh rieng truoc khi vao shell/sidebar.
- Dong/thu nho cua so se an xuong system tray; chi nut `Thoat may chu` hoac menu tray moi dung server.
- Tham so cu van dung: `--url`, `--database`, `--log-folder`, `--sample-data`.
- Day van la tray application theo user session, chua phai Windows Service tu chay truoc khi dang nhap Windows.
- File log ky thuat `quanlyhoso-yyyyMMdd.log` tu dong chi giu 30 ngay; cleanup moi ngay mot lan va khong xoa file sai mau/khac ten.
