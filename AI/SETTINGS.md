# Cai dat

Chi tiet theo popup/chuc nang:
- `AI/pages/SETTINGS_HOME.md`
- `AI/popups/SETTINGS_CATALOG_DIALOG.md`
- `AI/popups/SETTINGS_GENERAL_DIALOG.md`
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
- user/catalog/system log/backup/restore trong `AppDataService`.

Dang co:
- quan ly danh muc
- popup quan ly user
- cai dat chung: DB path, log folder, `DataAccessMode`, `AdminMachineName`, `AdminServerUrl`
- system logs
- backup/restore
- update software
