# Page - Cai dat home

Dung khi task lien quan trang Cai dat tong.

Files:
- `Views\Settings\SettingsView.xaml`
- `Views\Settings\SettingsView.xaml.cs`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`

Home cards/open commands:
- catalog cards: `OpenCatalogDialogCommand`
- system logs: `OpenSystemLogDialogCommand`
- user management: `OpenUserManagementDialogCommand`
- guide: `OpenGuideDialogCommand`
- backup server-side: `BackupNowCommand`

Notes:
- Dialogs nam chung trong `SettingsView.xaml` bang overlay `Grid Background="#6606164A"`.
- Drag/drop catalog values nam trong `SettingsView.xaml.cs`.
- Khong con UI cai dat DB/log/url tren WPF. Cac thong so do thuoc server (`QuanLyHoSo.Server` args/config).
