# Phan loai & xu ly

Chi tiet theo chuc nang:
- `AI/pages/PROCESSING_QUEUE.md`
- `AI/pages/PROCESSING_DETAIL.md`

File can mo:
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordProcessingView.xaml`

Service methods:
- `GetProcessingQueueMetrics`
- `GetProcessingQueueRecords`
- `CountProcessingQueueRecords`
- `GetProcessingRecordDetail`
- `UpdateProcessingRecord`

Ghi chu:
- Officer chi sua ho so dung ten minh.
- Leader chi xem.
- Area filters trong ViewModel da la `ObservableCollection<AreaSelectionOption>`.
