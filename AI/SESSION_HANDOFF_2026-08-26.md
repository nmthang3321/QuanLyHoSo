# Session handoff - QuanLyHoSo

Cap nhat: 2026-09-01

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
- Officer/can bo khong thay muc Nhap du lieu, khong them/xoa ho so; chi xem/chinh sua/phan loai theo quyen va khong duoc lui workflow ve buoc truoc phan cong.
- Attachment LAN hien moi luu metadata/path; chua co upload/copy file vat ly tu client len server.
