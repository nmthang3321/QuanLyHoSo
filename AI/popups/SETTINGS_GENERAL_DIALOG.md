# Popup - Settings general dialog

Dung khi task lien quan cai dat chung DB/log/LAN.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`
- `Infrastructure\Configuration\AppPathSettings.cs`

State/commands:
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

