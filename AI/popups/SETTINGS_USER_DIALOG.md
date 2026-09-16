# Popup - Settings user management

Files: Settings XAML/ViewModel, auth models, `AppDataService`, and `AuthContext`.

State/commands cover open/close, new/edit/save, and status toggle.

- Only Admin can manage users (`CanManageUsers`).
- Current user cannot be deleted/deactivated. Accounts are deactivated with `IsActive = 0`, not physically deleted; inactive rows are dimmed and sorted last.
- At least one active Admin must remain.
- Creating a user or assigning a new password sets `MustChangePassword = 1`.
- Trimmed case-insensitive `UserName` and `DisplayName` must be unique. ViewModel validates early; `AppDataService.SaveUser` enforces again for concurrent clients.
- Officer name is a ComboBox bound to active `ProcessorName` catalog values, not free text. When editing a legacy/inactive name, the current value is temporarily added so it is not lost.
