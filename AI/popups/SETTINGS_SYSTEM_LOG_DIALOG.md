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

Notes:
- DB bang `SystemLogs`.
- Ghi log qua helper `WriteDatabaseLog(...)` trong `AppDataService.cs`.
- Khong log thao tac doc/filter/xem chi tiet/export/backup refresh UI.

