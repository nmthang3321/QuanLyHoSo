# Feature - Export Excel

Dung khi task lien quan xuat Excel/preview/export data.

Files:
- `Views\Records\RecordListView.xaml`
- `ViewModels\RecordListViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetExportPreview`
- `BuildExportWhere`
- `GetFilteredRecords` neu can dung chung filter

Notes:
- Export nam trong trang Danh sach ho so, page `Export` rieng da bo.
- Export da chuyen async/background de tranh block UI.
- `_isExporting` tranh bam lap khi dang export.

