# Feature - Backup and restore

Files: `Views\Settings\SettingsView.xaml`, `ViewModels\SettingsViewModel.cs`, `Infrastructure\Data\AppDataService.cs`, `LanApiModels.cs`, `LanDataServer.cs`, and `AppPathSettings.cs`.

Service methods: `BackupDatabase`, `CreateBackupFile`, `DownloadBackupFile`, `GetBackupFilePath`, `RestoreDatabaseFromUpload`, `RestoreDatabaseFromFile`, and `ValidateDatabaseFile`.

Behavior:
- Backup uses SQLite's online `BackupDatabase` API, not `File.Copy` on a live database.
- The Admin-only Settings card can choose a local destination for downloaded backups and a `.db` source for restore.
- Backup flow: client calls `settings/backup/create`; server creates a safe backup; client downloads it through `settings/backup/download`.
- Restore flow: client uploads the selected file; server writes `.restore_upload_<guid>.db`, validates it, creates `quanlyhoso_before_restore_<timestamp>.db`, restores with SQLite backup, runs `PRAGMA quick_check`, and deletes the upload temp file in `finally`.
- Server backup folder: `%LocalAppData%\QuanLyHoSo\Backup`.
- Automatic backup is checked at server startup and hourly. A new `quanlyhoso_auto_yyyyMMdd_HHmmss.db` is created when the newest automatic backup is at least seven days old.
- Each check retains only the ten newest `quanlyhoso_*.db` files across automatic, manual, legacy, and pre-restore safety backups.
- Backup is initiated from WPF Settings; the server window uses that former action area for Admin-account reset.
