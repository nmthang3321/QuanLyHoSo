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
- `ToggleUserStatusCommand`

Rules:
- Chi Admin thay/quan ly user (`CanManageUsers`).
- Khong xoa/deactivate current user.
- Tai khoan chi bi khoa bang `IsActive = 0`, khong xoa vinh vien.
- Tai khoan bi khoa hien mo va nam cuoi danh sach; khong co cot thao tac rieng.
- Nut trang thai cho phep khoa/mo khoa tai khoan dang chon.
- Luon giu lai it nhat mot Admin dang hoat dong.
- Bang nguoi dung hien luoi ngang va doc mau nhat de tach cot ro rang.
- Tao user moi hoac nhap mat khau moi cho user dang sua se set `MustChangePassword = 1`; user phai doi mat khau o lan dang nhap tiep theo.
- Khong cho trung `UserName` hoac `DisplayName` sau khi trim va so sanh khong phan biet hoa/thuong; khi trung tai khoan da khoa thi huong dan chon tai khoan cu de mo khoa.
- Kiem tra trung duoc thuc hien ca trong `SettingsViewModel` de bao som va `AppDataService.SaveUser` de bao ve khi nhieu client cung thao tac.
- Truong ten can bo la ComboBox bind `UserProcessorNames`, nap tu catalog active `ProcessorName`; khong nhap ten tu do.
- Khi sua tai khoan cu co ten khong con active trong catalog, ten hien tai duoc them tam vao danh sach de tranh mat du lieu.
