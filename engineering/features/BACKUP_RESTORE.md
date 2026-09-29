# Feature - Backup and restore

Files: `Views\Settings\SettingsView.xaml`, `ViewModels\SettingsViewModel.cs`, `Infrastructure\Data\AppDataService.cs`, `Infrastructure\Network\LanApiModels.cs`, `Infrastructure\Network\LanDataServer.cs`, and `Infrastructure\Configuration\AppPathSettings.cs`.

Service methods include `BackupDatabase`, `CreateBackupFile`, `DownloadBackupFile`, `GetBackupFilePath`, `RestoreDatabaseFromUpload`, `RestoreDatabaseFromFile`, and `ValidateDatabaseFile`.

Behavior:

- The normal backup format is `.qlhbackup`. It is a complete server-data package containing a consistent SQLite backup and every server-managed file under `QuanLyHoSoFiles`, including attachments and generated documents.
- Backup uses SQLite's online `BackupDatabase` API to capture the live database safely; it does not copy an open database file directly.
- The Admin-only Settings card lets the operator choose a local destination for a downloaded backup and a source file for restore.
- Backup flow: the client calls `settings/backup/create`; the server builds the package; the client downloads it through `settings/backup/download`.
- Restore validates and stages the package before replacing live data. The server creates a full pre-restore safety package, restores the database and managed files, runs `PRAGMA quick_check`, and removes temporary restore data in `finally`.
- Legacy `.db` files remain accepted for backward compatibility. A `.db` backup/restore contains the database only and cannot recover attachments or generated documents.
- Server backup folder: `%LocalAppData%\QuanLyHoSo\Backup` unless server path configuration overrides the base location.
- Automatic backup is checked at server startup and hourly. A new `quanlyhoso_auto_yyyyMMdd_HHmmss.qlhbackup` is created when the newest automatic backup is at least seven days old.
- Retention keeps the ten newest recognized `.qlhbackup` and legacy `.db` backups across automatic, manual, and pre-restore safety backups.
- Backup is initiated from WPF Settings; the server window uses that former action area for Admin-account reset.

Operational rule: use `.qlhbackup` for disaster recovery. Keep `.db` support only for importing or restoring older database-only backups.
