# Page - Settings guide

Dung khi task lien quan trang Huong dan mo tu Cai dat.

Files:
- `Views\Settings\SettingsGuideView.xaml`
- `ViewModels\SettingsGuideViewModel.cs`
- `ViewModels\SettingsViewModel.cs`
- `ViewModels\ShellViewModel.cs`

Flow:
- `SettingsViewModel.OpenGuideCommand` goi callback cua shell.
- Shell hien `SettingsGuideViewModel`, giu sidebar `Settings` duoc chon.
- `BackCommand` dieu huong ve trang `Settings`.
- Noi dung ngan gon, moi muc chi giu muc dich, cach lam va luu y quan trong; duoc tao rieng theo role `Admin`, `Leader`, `Officer`.
- Phan `Nhung dieu can biet` tom tat bo loc Danh sach ho so, bo loc Theo doi can bo, cong thuc hieu suat/dung han va cach dat KPI. Admin co them quan tri an toan; Officer co them cach cap nhat ho so duoc giao.
