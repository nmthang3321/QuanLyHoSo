# Feature - Backup/Restore

Dung khi task lien quan sao luu/khoi phuc DB.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`
- `Infrastructure\Configuration\AppPathSettings.cs`

Service methods:
- `BackupDatabase`
- `RestoreDatabaseFromFile`
- `ValidateDatabaseFile`

Notes:
- Backup dung SQLite `BackupDatabase` API thay vi `File.Copy` DB song.
- Restore tao safety backup truoc, restore bang SQLite backup API, sau do `PRAGMA quick_check`.
- Khi doi DB path trong cai dat chung, copy DB ban dau cung dung backup API.

