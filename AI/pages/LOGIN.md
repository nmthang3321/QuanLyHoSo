# Page - Login

Files: login and required-password-change views/ViewModels, `AuthModels.cs`, `AuthContext.cs`, and `AppDataService.AuthenticateUser`.

- Seeded account: `admin/admin123`.
- When `MustChangePassword` is true, Shell displays the password-change page outside the normal shell/sidebar. Successful change is required before role-default navigation.
- New accounts, Admin-reset passwords, and built-in Admin reset from the server require a password change. A user-initiated successful change clears the flag.
- Client does not ping on startup; it connects when the user signs in.
- Authentication/connection errors appear through `ErrorMessage`/`HasError`, not MessageBox.
- Forgot-password text is `Vui lòng liên hệ Admin để cấp lại mật khẩu.`
