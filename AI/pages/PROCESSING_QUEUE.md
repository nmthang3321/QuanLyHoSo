# Page - Phan loai & xu ly queue

Dung khi task lien quan danh sach/queue xu ly ho so.

Files:
- `Views\Records\RecordProcessingView.xaml`
- `ViewModels\RecordProcessingViewModel.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetProcessingQueueMetrics`
- `GetProcessingQueueRecords`
- `CountProcessingQueueRecords`

Notes:
- Officer chi sua ho so dung ten minh.
- Leader chi xem.
- Area filters trong VM da la `ObservableCollection<AreaSelectionOption>`.
- Chi tiet/cap nhat xu ly doc `AI/pages/PROCESSING_DETAIL.md`.

