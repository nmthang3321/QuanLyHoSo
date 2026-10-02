# Page - Settings home

Files: `SettingsView.xaml[.cs]`, `SettingsViewModel.cs`, and `SettingsModels.cs`.

Commands open catalog, system-log, and user-management overlays; choose/create backup; and choose/restore a database. The former Guide button is removed, so `SettingsGuideViewModel/View` is currently unreachable from UI.

- Remaining dialogs are overlays inside `SettingsView.xaml`; catalog drag/drop is in code-behind.
- Settings exposes six editable catalog groups. The internal `Priority` (`Mức độ vụ việc`) catalog is not shown because its canonical values drive processing-card calculations and are protected by the data service.
- DB/log/API URL configuration is server-owned and no longer exposed here.
- Admin sees all cards. Officer/Leader see Software information, software update, and Quick actions; catalog/backup/user administration is hidden. The update card title is role-neutral. Officer/Leader can update only the current Client. Admin can choose either Client only or Server and Client. A combined update uses `QuanLyHoSo-Server-<version>.zip` on the Server and `QuanLyHoSo-Client-<version>.zip` on the current workstation; versions remain independently comparable.
- Server updates are authorized again by the LAN session, require Windows elevation on the Server machine, restart `QuanLyHoSo.Server.exe`, and preserve its original command-line arguments. The Client package is downloaded before the Server restarts because an independent Server restart clears in-memory login sessions. After either update scope completes, the Client restarts at Login. Update-script failures are written under `%LocalAppData%\QuanLyHoSo\Logs` and make a best-effort attempt to reopen the previous executable.
- The backup card has separate automatic-status, manual-backup, and restore sections. The normal `.qlhbackup` package includes the database, attachments, and generated documents. Legacy `.db` restore remains available but is database-only. Long paths use ellipsis/tooltips; restore uses a light warning palette. The server folder retains the ten newest recognized backups across types.
- Full-package restore is portable across Server data roots: attachment metadata containing absolute paths from the source machine is rebased to the restored files under the current Server storage root. Restore still requires managed attachment folders/files not to be held open by Explorer or another application while the directory swap runs.
- Admin layout: Backup is left, Quick actions is right and top-aligned to content height. Officer/Leader keep Quick actions left and Software information right on the first row, with Update below.
- Cards use clear borders rather than direct DropShadowEffect. Accent colors: blue for actions/info, green for updates, orange for backup/restore.
- Layout rounding and pixel snapping are enabled. Typography follows shared `PageTitleText`, `SectionTitleText`, and Segoe UI hierarchy.
