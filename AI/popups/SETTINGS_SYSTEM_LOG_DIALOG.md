# Popup - Settings system log

Dung khi task lien quan nhat ky he thong.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`
- `Infrastructure\Data\AppDataService.cs`

State/commands:
- `IsSystemLogDialogOpen`
- `OpenSystemLogDialogCommand`
- `CloseSystemLogDialogCommand`
- `RefreshSystemLogsCommand` vẫn có trong ViewModel; nút Làm mới đã bỏ khỏi popup theo yêu cầu 2026-09-14.

Notes:
- DB bang `SystemLogs`.
- Ghi log qua helper `WriteDatabaseLog(...)` trong `AppDataService.cs`.
- Khong log thao tac doc/filter/xem chi tiet/export/backup refresh UI.
