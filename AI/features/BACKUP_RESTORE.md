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
- Server kiem tra sao luu tu dong ngay khi khoi dong va moi 1 gio trong luc chay. Neu ban tu dong moi nhat da du 7 ngay thi tao file `quanlyhoso_auto_yyyyMMdd_HHmmss.db` trong `%LocalAppData%\QuanLyHoSo\Backup`.
- Thu muc backup tren server chi giu 10 file `quanlyhoso_*.db` moi nhat, tinh chung backup tu dong, backup thu cong va safety backup truoc khoi phuc. Viec don dep chay trong moi lan kiem tra sao luu tu dong.
- Nut sao luu truc tiep tren cua so `QuanLyHoSo.Server` da duoc thay bang reset tai khoan Admin; Admin sao luu tu card Settings trong app WPF.
