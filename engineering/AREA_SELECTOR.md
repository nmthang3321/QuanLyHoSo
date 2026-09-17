# Hierarchical area selector

The detailed reference is `engineering/features/AREA_SELECTOR.md`. Read that file first when changing this feature.

Use this map for tasks involving the `Địa bàn` field on record intake, the area filter on the record list, or the commune/province/ministry/provincial-police/out-of-province groups.

Main files:
- `Models\AreaSelectionModels.cs`
- `Infrastructure\Data\AppDataService.cs`
- `ViewModels\RecordInputViewModel.cs`
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordInputView.xaml[.cs]`
- `Views\Records\RecordListView.xaml[.cs]`

## Area groups

Ordered from the smallest to the largest scope:
- `Cấp xã`: 102 communes, wards, and special zones from `Areas`.
- `Cấp tỉnh`: `Tỉnh ủy An Giang`, `Ủy ban nhân dân tỉnh`, `Ban Nội chính Tỉnh ủy`, `Thanh tra tỉnh`.
- `Cấp bộ`: `C01`, `C02`, `C03`, `C04`, `X05`, `X06`.
- `Công an tỉnh`: `PC02`, `PC03`, `PC04`, `PX05`, `PX06`, `Đơn vị khác trong tỉnh`.
- `Đơn vị trong ngành ngoài tỉnh`: one option with the same name.

## Current behavior

Record intake:
- A button opens an inline panel on root `AreaOverlayCanvas`.
- Do not use `Popup` or `ContextMenu` for search because Vietnamese IME composition may appear at the top-left of the screen.
- The overlay does not stretch the page vertically and is not clipped by cards or sections.
- Code-behind positions it from `AreaDropDownButton` with `TransformToVisual(AreaOverlayCanvas)`. Do not use `TransformToAncestor`; the canvas is a sibling and that call crashes.
- The panel has search and clickable expandable group headers. Searching automatically expands groups and shows matching items only.
- Selecting an item sets `AreaName = option.FilterValue`.

Record list:
- The area filter uses root overlay `AreaFilterOverlayCanvas` with the same search and expandable groups.
- Users can select `Tất cả`, a group, or a child item. Clicking a group applies the filter and toggles its children.

Data/filter rules:
- Intake and record-list pages use `FilteredAreas`, `AreaSearchText`, and `AreaSelectionOptions.Filter/Flatten`.
- `AppDataService.Initialize()` calls `EnsureStandardOrganizationAreas(connection)`.
- `GetAreaNames()` formats communes/wards/special zones as `"AreaType Name"`; organization units return `Name`.
- Record, export, and processing-queue filters use `AddOptionalAreaFilter()`.
- Manual save/update keeps `AreaName = $areaName` unchanged.

Latest verification:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/record-list-area-overlay
```
