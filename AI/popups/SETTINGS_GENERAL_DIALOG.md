# Popup - General Settings (legacy)

This popup was removed after introducing the separate server. Database path, log folder, and API URL are configured through `QuanLyHoSo.Server` startup/configuration, not the client UI.

Legacy ViewModel state/commands may remain: `IsGeneralSettingsDialogOpen`, `OpenGeneralSettingsDialogCommand`, and `CloseGeneralSettingsDialogCommand`. Fields were DB path, log folder, `DataAccessMode`, `AdminMachineName`, and `AdminServerUrl`.

See `AI/infra/LAN_API.md`. Do not reintroduce this UI without an explicit requirement.
