# Popup - Record detail modal

Dung khi task lien quan popup/overlay chi tiet ho so trong Danh sach ho so.

Files:
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `ViewModels\RecordListViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`

Service method:
- `GetRecordForm`

Notes:
- Modal nam cuoi `RecordListView.xaml`, overlay trong root Grid.
- Button dong bind `CloseDetailCommand`.
- Attachments hien trong detail tu `SelectedRecordDetail.Attachments`.

