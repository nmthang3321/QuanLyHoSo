# Cai dat

Chi tiet theo popup/chuc nang:
- `AI/pages/SETTINGS_HOME.md`
- `AI/popups/SETTINGS_CATALOG_DIALOG.md`
- `AI/popups/SETTINGS_GUIDE_DIALOG.md`
- `AI/popups/SETTINGS_SYSTEM_LOG_DIALOG.md`
- `AI/popups/SETTINGS_USER_DIALOG.md`
- `AI/features/BACKUP_RESTORE.md`
- `AI/features/CATALOGS.md`
- `AI/features/AUDIT_LOG.md`

File can mo:
- `ViewModels\SettingsViewModel.cs`
- `Views\Settings\SettingsView.xaml`
- `Models\SettingsModels.cs`

Service methods:
- user/catalog/system log/backup trong `AppDataService`.

Dang co:
- quan ly danh muc
- popup quan ly user
- system logs
- backup DB server-side qua `CreateBackupFile` / route `settings/backup/create`
- update software

Khong con tren WPF Settings:
- popup cai dat chung DB/log/url. DB/log/API URL cau hinh o server bang tham so chay hoac config server.
- restore DB tu client. Restore nen thuc hien o server/bao tri.
