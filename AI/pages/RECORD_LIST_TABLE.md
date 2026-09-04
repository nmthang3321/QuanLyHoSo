# Page - Danh sach ho so table

Dung khi task lien quan bang ho so, phan trang, thao tac nhanh trong danh sach.

Files:
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordListRowViewModel.cs`

Notes:
- DataGrid bind `Records`.
- Table height bind `TableHeight`.
- Mouse wheel DataGrid forward ve `RecordListScrollViewer`.
- Detail modal doc `AI/popups/RECORD_DETAIL_MODAL.md`.
- Export doc `AI/features/EXPORT_EXCEL.md`.
- Row action `CanEdit` = `AuthContext.CanEditRecord(record.ProcessorName)`. Khong check `AppPathSettings.Current.IsClientMode` tai row nua.
