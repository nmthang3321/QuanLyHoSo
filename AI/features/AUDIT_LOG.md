# Feature - Audit/system log

Dung khi task lien quan ghi log DB/nhat ky he thong.

Files:
- `Infrastructure\Data\AppDataService.cs`
- `ViewModels\SettingsViewModel.cs`
- `Views\Settings\SettingsView.xaml`
- `Models\SettingsModels.cs`

DB:
- Bang `SystemLogs`.

Notes:
- Ghi log qua helper `WriteDatabaseLog(...)`.
- Dang ghi cho them/sua/xoa/sap xep danh muc, them/sua/xoa ho so, cap nhat xu ly/trang thai.
- Khong log thao tac doc du lieu, filter, xem chi tiet, export file, backup file, refresh UI.
- Khong log seed/init.

