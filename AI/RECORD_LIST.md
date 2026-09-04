# Danh sach ho so

Chi tiet theo chuc nang:
- `AI/pages/RECORD_LIST_FILTERS.md`
- `AI/pages/RECORD_LIST_TABLE.md`
- `AI/popups/RECORD_DETAIL_MODAL.md`
- `AI/features/AREA_SELECTOR.md`
- `AI/features/EXPORT_EXCEL.md`

File can mo:
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordListRowViewModel.cs`
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `Models\AreaSelectionModels.cs` neu cham toi dia ban.
- `AI/AREA_SELECTOR.md` neu cham toi dia ban.

Service methods:
- `GetFilteredRecords`
- `CountFilteredRecords`
- `GetExportPreview`
- `DeleteRecord`
- `GetRecordForm`

Ghi chu:
- Export Excel nam trong trang nay. Page `Export` rieng da bo.
- Bo loc dia ban dung root overlay `AreaFilterOverlayCanvas`, search text va group bung/thu.
- Chon duoc `Tat ca`, group, hoac item con. Click group vua set filter theo group vua bung/thu de xem item con.
- Nut sua ho so tren tung dong dua theo `AuthContext.CanEditRecord(record.ProcessorName)`, khong khoa theo client mode nua. Khi WPF chay `Client`, sua/luu ho so di qua LAN API.
