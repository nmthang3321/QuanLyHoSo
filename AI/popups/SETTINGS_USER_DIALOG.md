# Popup - Settings user management

Dung khi task lien quan quan ly user.

Files:
- `Views\Settings\SettingsView.xaml`
- `ViewModels\SettingsViewModel.cs`
- `Models\AuthModels.cs`
- `Infrastructure\Data\AppDataService.cs`
- `Infrastructure\Security\AuthContext.cs`

State/commands:
- `IsUserManagementDialogOpen`
- `OpenUserManagementDialogCommand`
- `CloseUserManagementDialogCommand`
- `NewUserCommand`
- `EditUserCommand`
- `SaveUserCommand`
- `DeleteUserCommand`

Rules:
- Chi Admin thay/quan ly user (`CanManageUsers`).
- Khong xoa/deactivate current user trong `DeleteUser`.

