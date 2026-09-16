# Settings

Detailed references: `AI/pages/SETTINGS_HOME.md`, the Settings popup files under `AI/popups/`, and `AI/features/BACKUP_RESTORE.md`, `CATALOGS.md`, and `AUDIT_LOG.md`.

Open `ViewModels\SettingsViewModel.cs`, `Views\Settings\SettingsView.xaml`, and `Models\SettingsModels.cs`. User, catalog, system-log, backup, restore, and update operations are implemented through `AppDataService`.

Current features include catalog management, user management, system logs, server-side backup/restore, and software updates.

Removed from WPF Settings: the legacy database/log/API URL configuration dialog. Those values belong to server startup/configuration.
