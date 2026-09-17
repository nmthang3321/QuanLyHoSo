# Feature - Audit and system log

Use for database audit-log tasks.

Files: `Infrastructure\Data\AppDataService.cs`, `ViewModels\SettingsViewModel.cs`, `Views\Settings\SettingsView.xaml`, and `Models\SettingsModels.cs`.

Database table: `SystemLogs`.

- Write audit entries through `WriteDatabaseLog(...)`.
- Current coverage includes catalog add/edit/delete/reorder, record add/edit/delete, and processing/status updates.
- Read, filter, detail view, export, backup-file creation, and UI refresh operations are not logged.
- Seed and initialization operations are not logged.
