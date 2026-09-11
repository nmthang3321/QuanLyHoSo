# Page - Cai dat home

Dung khi task lien quan trang Cai dat tong.

Files:
- `Views\Settings\SettingsView.xaml`
- `Views\Settings\SettingsView.xaml.cs`
- `ViewModels\SettingsViewModel.cs`
- `Models\SettingsModels.cs`

Home cards/open commands:
- catalog cards: `OpenCatalogDialogCommand`
- system logs: `OpenSystemLogDialogCommand`
- user management: `OpenUserManagementDialogCommand`
- nut guide da duoc go khoi trang Cai dat; `SettingsGuideViewModel`/`SettingsGuideView` hien khong duoc mo tu UI.
- backup: `ChooseBackupFolderCommand`, `BackupNowCommand`
- restore: `ChooseRestoreFileCommand`, `RestoreDataCommand`

Notes:
- Cac dialog con lai nam chung trong `SettingsView.xaml` bang overlay `Grid Background="#6606164A"`; Huong dan khong con la dialog.
- Drag/drop catalog values nam trong `SettingsView.xaml.cs`.
- Khong con UI cai dat DB/log/url tren WPF. Cac thong so do thuoc server (`QuanLyHoSo.Server` args/config).
- `Admin` thay toan bo card. `Officer` va `Leader` chi thay `Thong tin phan mem`, `Cap nhat phan mem`, `Thao tac nhanh`; card danh muc/sao luu va muc `Nguoi dung & phan quyen` bi an.
- Card sao luu cua Admin cho chon thu muc luu ban sao tren may WPF, chon file `.db` de khoi phuc, va hien trang thai/lan sao luu gan nhat. Backup duoc tao tren server roi tai ve client; file khoi phuc duoc gui len server de xu ly.
- Home dung Grid hai cot bang nhau. Voi `Officer`/`Leader`: hang dau la `Thao tac nhanh` ben trai va `Thong tin phan mem` ben phai, hai card can cung chieu cao; `Cap nhat phan mem` nam o hang tiep theo ben trai. Voi `Admin`, cac card quan tri van hien day du theo Grid.
- Card settings dung vien ro, khong gan `DropShadowEffect` truc tiep len card de tranh lam mo chu trong WPF; header co thanh nhan/icon badge. Mau nhan: xanh duong cho thao tac/thong tin, xanh la cho cap nhat, cam cho sao luu. Quick action co hover nen xanh nhat.
- `SettingsView` bat layout rounding va device-pixel snapping; khong ep che do render text rieng de title hien thi giong cac trang khac. Chu mo ta thao tac nhanh dung co 12 de dong bo voi format chu phu cua ung dung.
- Do dam chu theo cap bac: noi dung `Medium`, title card/dialog dung `SectionTitleText` (`SemiBold`) nhu title `HO SO MOI NHAT`, title `CAI DAT` dung dung `PageTitleText` nhu header cac trang khac; van giu Segoe UI va co chu theo he thong chung.
