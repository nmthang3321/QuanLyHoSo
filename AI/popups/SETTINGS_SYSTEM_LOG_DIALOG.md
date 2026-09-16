# Popup - Settings system log

Files: Settings XAML/ViewModel/models and `AppDataService`.

State/commands: `IsSystemLogDialogOpen`, open/close, and legacy `RefreshSystemLogsCommand`; the Refresh button was removed on 2026-09-14.

Entries come from `SystemLogs` and are written through `WriteDatabaseLog(...)`. Read/filter/detail/export/backup/UI-refresh operations are not logged.
