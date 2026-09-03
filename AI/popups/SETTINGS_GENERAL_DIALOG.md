# Popup - Settings general dialog (legacy)

Popup nay da bi go khoi WPF Settings sau khi tach server rieng. DB/log/API URL duoc cau hinh o `QuanLyHoSo.Server` bang tham so chay hoac config server, khong chinh trong app client nua.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`
- `Infrastructure\Configuration\AppPathSettings.cs`

State/commands legacy con trong ViewModel neu can cleanup sau:
- `IsGeneralSettingsDialogOpen`
- `OpenGeneralSettingsDialogCommand`
- `CloseGeneralSettingsDialogCommand`

Fields:
- DB path
- log folder
- `DataAccessMode`
- `AdminMachineName`
- `AdminServerUrl`

Notes:
- Neu cham LAN doc `AI/infra/LAN_API.md`.
- Khong them lai UI nay vao WPF client tru khi co yeu cau ro.
