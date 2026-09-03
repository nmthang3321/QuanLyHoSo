# AI index - QuanLyHoSo

Cap nhat: 2026-09-01

Doc file nay truoc, sau do chi doc file dung voi task.

Session handoff nam tai `AI/SESSION_HANDOFF_2026-08-26.md`. Context AI chi dung cac file trong thu muc `AI/`.

## Routing tong quan

- Tong quan repo, verify, role: `AI/COMMON.md`
- LAN/client/server summary: `AI/LAN.md`
- Dashboard summary: `AI/DASHBOARD.md`
- Nhap du lieu summary: `AI/RECORD_INPUT.md`
- Danh sach ho so summary: `AI/RECORD_LIST.md`
- Phan loai & xu ly summary: `AI/PROCESSING.md`
- Theo doi can bo summary: `AI/STAFF_TRACKING.md`
- Cai dat summary: `AI/SETTINGS.md`
- DB/schema summary: `AI/DATABASE.md`
- Quy tac sua code/build/git: `AI/RULES.md`

## Pages

- Login: `AI/pages/LOGIN.md`
- Dashboard overview: `AI/pages/DASHBOARD_OVERVIEW.md`
- Nhap du lieu form: `AI/pages/RECORD_INPUT_FORM.md`
- Danh sach bo loc: `AI/pages/RECORD_LIST_FILTERS.md`
- Danh sach table: `AI/pages/RECORD_LIST_TABLE.md`
- Xu ly queue: `AI/pages/PROCESSING_QUEUE.md`
- Xu ly detail/update: `AI/pages/PROCESSING_DETAIL.md`
- Theo doi can bo: `AI/pages/STAFF_TRACKING.md`
- Cai dat home: `AI/pages/SETTINGS_HOME.md`

## Popups/overlays

- Dashboard custom date range: `AI/popups/DASHBOARD_DATE_RANGE.md`
- Record detail modal: `AI/popups/RECORD_DETAIL_MODAL.md`
- Settings catalog dialog: `AI/popups/SETTINGS_CATALOG_DIALOG.md`
- Settings general dialog legacy/removed from WPF UI: `AI/popups/SETTINGS_GENERAL_DIALOG.md`
- Settings guide dialog: `AI/popups/SETTINGS_GUIDE_DIALOG.md`
- Settings system log dialog: `AI/popups/SETTINGS_SYSTEM_LOG_DIALOG.md`
- Settings user management dialog: `AI/popups/SETTINGS_USER_DIALOG.md`

## Features

- Area selector: `AI/features/AREA_SELECTOR.md`
- Attachments: `AI/features/ATTACHMENTS.md`
- Audit/system log: `AI/features/AUDIT_LOG.md`
- Backup/restore: `AI/features/BACKUP_RESTORE.md`
- Catalogs: `AI/features/CATALOGS.md`
- Export Excel: `AI/features/EXPORT_EXCEL.md`
- Navigation/back/sidebar: `AI/features/NAVIGATION.md`

## Infra

- Build/git: `AI/infra/BUILD_GIT.md`
- AppDataService routing: `AI/infra/DATA_SERVICE.md`
- Database schema: `AI/infra/DATABASE_SCHEMA.md`
- LAN API: `AI/infra/LAN_API.md`

## Luu y moi nhat

- Selector dia ban dang co thay doi UI quan trong, doc `AI/features/AREA_SELECTOR.md` neu task cham toi dia ban.
- Build verify nen dung output rieng de tranh exe dang chay bi khoa:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

- Warning `NETSDK1138` ve `.NET 5.0-windows` het support la warning cu. Build OK neu 0 error.
