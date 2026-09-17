# Page - Processing queue

Files: `RecordProcessingView.xaml`, `RecordProcessingViewModel.cs`, and `AppDataService.cs`.

Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, and `CountProcessingQueueRecords`.

- Officer edits only records assigned to their display name; Leader is read-only.
- Area filters are `ObservableCollection<AreaSelectionOption>`.
- The `HighPriority` card and filter match the protected `Priority` values `Nghiêm trọng`, `Rất nghiêm trọng`, and `Đặc biệt nghiêm trọng`. This internal catalog is seeded but is not editable from Settings.
- See `engineering/pages/PROCESSING_DETAIL.md` for detail/update behavior.
