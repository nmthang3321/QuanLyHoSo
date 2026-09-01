# Page - Login

Dung khi task lien quan dang nhap/dang xuat/auth UI.

Files:
- `Views\Auth\LoginView.xaml`
- `Views\Auth\LoginView.xaml.cs`
- `ViewModels\LoginViewModel.cs`
- `Models\AuthModels.cs`
- `Infrastructure\Security\AuthContext.cs`
- `Infrastructure\Data\AppDataService.cs` method `AuthenticateUser`

Behavior:
- User mac dinh seed: `admin/admin123`.
- Sau login, `ShellViewModel` tao page VM va set role/current user.
- Loi ket noi LAN client co the hien MessageBox tu `ShellViewModel`.

