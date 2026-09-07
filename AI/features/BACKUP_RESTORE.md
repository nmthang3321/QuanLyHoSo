# Feature - Backup/Restore

Dung khi task lien quan sao luu/khoi phuc DB.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`
- `Infrastructure\Network\LanApiModels.cs`
- `Infrastructure\Network\LanDataServer.cs`
- `Infrastructure\Configuration\AppPathSettings.cs`

Service methods:
- `BackupDatabase`
- `CreateBackupFile`
- `DownloadBackupFile`
- `GetBackupFilePath`
- `RestoreDatabaseFromUpload`
- `RestoreDatabaseFromFile`
- `ValidateDatabaseFile`

Notes:
- Backup dung SQLite `BackupDatabase` API thay vi `File.Copy` DB song.
- Card `Sao luu du lieu` chi hien voi Admin. Admin co the chon thu muc luu tren may dang chay WPF va chon file `.db` de khoi phuc.
- Luong sao luu: client goi `settings/backup/create` de server tao backup an toan, sau do tai file qua `settings/backup/download` ve thu muc Admin da chon.
- Luong khoi phuc: client doc file `.db`, gui noi dung qua `settings/backup/restore`; server luu file tam, kiem tra database, tao safety backup, restore bang SQLite backup API, chay `PRAGMA quick_check`, roi xoa file tam.
- Safety backup truoc khi khoi phuc nam trong `%LocalAppData%\QuanLyHoSo\Backup` tren may server.
