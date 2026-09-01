# Page - Danh sach ho so filters

Dung khi task lien quan bo loc tren trang Danh sach ho so.

Files:
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `ViewModels\RecordListViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetFilteredRecords`
- `CountFilteredRecords`
- `BuildExportWhere`
- `AddOptionalAreaFilter`

Filters:
- tu ngay/den ngay
- trang thai
- loai vu viec
- linh vuc
- dia ban
- nguoi xu ly
- tu khoa
- sap xep

Notes:
- Nut `Bo loc` bind `IsFilterPanelOpen`.
- Area filter doc `AI/features/AREA_SELECTOR.md`.
- Nut `Xem du lieu` chay `ApplyFilterCommand`.
- Nut `Dat lai` chay `ResetFilterCommand`.

