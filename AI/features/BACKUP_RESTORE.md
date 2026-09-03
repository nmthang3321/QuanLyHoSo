# Feature - Backup/Restore

Dung khi task lien quan sao luu/khoi phuc DB.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`
- `Infrastructure\Configuration\AppPathSettings.cs`

Service methods:
- `BackupDatabase`
- `CreateBackupFile`
- `RestoreDatabaseFromFile`
- `ValidateDatabaseFile`

Notes:
- Backup dung SQLite `BackupDatabase` API thay vi `File.Copy` DB song.
- WPF Settings hien chi co backup server-side: admin bam Sao luu ngay -> client goi `settings/backup/create` -> server tao file trong `%LocalAppData%\QuanLyHoSo\Backup` tren may server.
- Restore DB khong con hien tren WPF client. Neu can restore, thuc hien tren server/bao tri de server doc duoc file backup.
- `RestoreDatabaseFromFile` van con trong service cho ky thuat/bao tri: tao safety backup truoc, restore bang SQLite backup API, sau do `PRAGMA quick_check`.
