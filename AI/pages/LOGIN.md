# Page - Login

Dung khi task lien quan dang nhap/dang xuat/auth UI.

Files:
- `Views\Auth\LoginView.xaml`
- `Views\Auth\LoginView.xaml.cs`
- `Views\Auth\RequiredPasswordChangeView.xaml`
- `ViewModels\RequiredPasswordChangeViewModel.cs`
- `ViewModels\LoginViewModel.cs`
- `Models\AuthModels.cs`
- `Infrastructure\Security\AuthContext.cs`
- `Infrastructure\Data\AppDataService.cs` method `AuthenticateUser`

Behavior:
- User mac dinh seed: `admin/admin123`.
- Sau login, neu `MustChangePassword = true`, `ShellViewModel` chi hien form doi mat khau ben ngoai shell/sidebar. Doi thanh cong moi vao trang mac dinh theo role.
- Tai khoan moi, tai khoan duoc Admin cap lai mat khau va built-in `admin` duoc reset tu Server deu bat buoc doi mat khau.
- Mat khau do chinh user doi se set `MustChangePassword = 0`.
- Client khong ping server khi mo app; chi ket noi khi nguoi dung dang nhap.
- Loi dang nhap/ket noi server hien qua `ErrorMessage`/`HasError` trong form, khong hien MessageBox.
- Quen mat khau huong dan lien he Admin; neu Admin cung mat quyen truy cap thi reset built-in `admin` tai may Server.
