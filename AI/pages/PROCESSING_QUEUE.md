# Page - Processing queue

Files: `RecordProcessingView.xaml`, `RecordProcessingViewModel.cs`, and `AppDataService.cs`.

Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, and `CountProcessingQueueRecords`.

- Officer edits only records assigned to their display name; Leader is read-only.
- Area filters are `ObservableCollection<AreaSelectionOption>`.
- See `AI/pages/PROCESSING_DETAIL.md` for detail/update behavior.
