# Feature - Area selector

Dung khi task lien quan dia ban/group/search/select.

Doc them:
- `AI/pages/RECORD_INPUT_FORM.md`
- `AI/pages/RECORD_LIST_FILTERS.md`

Files:
- `Models\AreaSelectionModels.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `ViewModels\RecordInputViewModel.cs`
- `ViewModels\RecordListViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`

UI rules:
- Khong dung `Popup`/`ContextMenu` cho textbox search vi IME tieng Viet co the hien edit box o goc trai.
- Dung root overlay canvas:
  - Input: `AreaOverlayCanvas`, `AreaPanel`, `AreaSearchBox`.
  - List filter: `AreaFilterOverlayCanvas`, `AreaFilterPanel`, `AreaFilterSearchBox`.
- Tinh vi tri bang `TransformToVisual(canvas)`, khong dung `TransformToAncestor` neu canvas la sibling.
- Panel khong lam gian layout va khong bi card/section cat.
- Group header dung `StrongTextBrush`, chevron/count dung `MutedTextBrush`.

Data rules:
- `AreaSelectionOptions.Build(...)` tao group.
- `AreaSelectionOptions.Filter/Flatten` dung cho search.
- Input chi chon item con.
- List filter chon duoc `Tat ca`, group, item con.
- Click group tren list filter vua set group filter vua bung/thu.
- `AddOptionalAreaFilter()` trong `AppDataService` xu ly SQL group filter.

