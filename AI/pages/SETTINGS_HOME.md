# Page - Settings home

Files: `SettingsView.xaml[.cs]`, `SettingsViewModel.cs`, and `SettingsModels.cs`.

Commands open catalog, system-log, and user-management overlays; choose/create backup; and choose/restore a database. The former Guide button is removed, so `SettingsGuideViewModel/View` is currently unreachable from UI.

- Remaining dialogs are overlays inside `SettingsView.xaml`; catalog drag/drop is in code-behind.
- DB/log/API URL configuration is server-owned and no longer exposed here.
- Admin sees all cards. Officer/Leader see Software information, Software update, and Quick actions; catalog/backup/user administration is hidden.
- The backup card has separate automatic-status, manual-backup, and restore sections. Long paths use ellipsis/tooltips; restore uses a light warning palette. The server folder retains ten newest backups across types.
- Admin layout: Backup is left, Quick actions is right and top-aligned to content height. Officer/Leader keep Quick actions left and Software information right on the first row, with Update below.
- Cards use clear borders rather than direct DropShadowEffect. Accent colors: blue for actions/info, green for updates, orange for backup/restore.
- Layout rounding and pixel snapping are enabled. Typography follows shared `PageTitleText`, `SectionTitleText`, and Segoe UI hierarchy.
