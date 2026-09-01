# Dia ban hierarchical selector

Ban chi tiet moi nam o `AI/features/AREA_SELECTOR.md`. Uu tien doc file do khi sua code.

Dung khi task lien quan:
- field `Dia ban` trong trang nhap lieu
- bo loc `Dia ban` trong danh sach ho so
- group dia ban/cap xa/cap tinh/cap bo/cong an tinh/ngoai tinh

File chinh:
- `Models\AreaSelectionModels.cs`
- `Infrastructure\Data\AppDataService.cs`
- `ViewModels\RecordInputViewModel.cs`
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`

## Nhom dia ban

Thu tu tu cap nho den cap lon:
- `Cap xa`: danh sach 102 xa/phuong/dac khu tu bang `Areas`.
- `Cap tinh`: `Tinh uy An Giang`, `Uy ban nhan dan tinh`, `Ban Noi chinh Tinh uy`, `Thanh tra tinh`.
- `Cap bo`: `C01`, `C02`, `C03`, `C04`, `X05`, `X06`.
- `Cong an tinh`: `PC02`, `PC03`, `PC04`, `PX05`, `PX06`, `Don vi khac trong tinh`.
- `Don vi trong nganh ngoai tinh`: 1 option cung ten.

## Behavior hien tai

Trang nhap lieu:
- Button mo panel inline tren root overlay `AreaOverlayCanvas`.
- Khong dung `Popup`/`ContextMenu` cho search vi bo go tieng Viet/IME co the hien edit box o goc trai man hinh.
- Khong lam gian layout doc va khong bi card/section khac cat.
- Code-behind tinh vi tri theo `AreaDropDownButton` bang `TransformToVisual(AreaOverlayCanvas)`.
- Khong dung `TransformToAncestor` vi canvas la sibling, se crash.
- Panel co textbox search, group header bung/thu bang click.
- Khi dang search, group tu bung va chi hien item khop.
- Chon item set `AreaName = option.FilterValue`.

Trang danh sach ho so:
- Bo loc dia ban dung root overlay `AreaFilterOverlayCanvas`.
- Co textbox search va group bung/thu nhu trang nhap lieu.
- Co the chon `Tat ca`, group, hoac item con.
- Click group vua set filter theo group vua bung/thu de xem item con.

Data/filter:
- Trang nhap lieu va danh sach ho so dung `FilteredAreas`, `AreaSearchText`, `AreaSelectionOptions.Filter/Flatten`.
- `AppDataService.Initialize()` goi `EnsureStandardOrganizationAreas(connection)`.
- `GetAreaNames()` format xa/phuong/dac khu thanh `"AreaType Name"`; don vi to chuc tra ve `Name`.
- Cac filter record/export/processing queue dung `AddOptionalAreaFilter()`.
- Manual save/update `AreaName = $areaName` khong doi.

Verify gan nhat:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/record-list-area-overlay
```
