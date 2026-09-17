# Page - Settings guide (currently unreachable)

Files: `SettingsGuideView.xaml`, `SettingsGuideViewModel.cs`, `SettingsViewModel.cs`, and `ShellViewModel.cs`.

Historical flow: `OpenGuideCommand` called a Shell callback; Shell displayed `SettingsGuideViewModel` while keeping Settings selected; Back returned to Settings. Content was concise and role-specific for Admin, Leader, and Officer, covering filters, tracking, performance/on-time formulas, KPI, Admin safety, and assigned-record updates.

The Settings Guide button is currently removed from the UI.
