# Classification and processing

Detailed references: `engineering/pages/PROCESSING_QUEUE.md` and `engineering/pages/PROCESSING_DETAIL.md`.

Open `ViewModels\RecordProcessingViewModel.cs` and `Views\Records\RecordProcessingView.xaml`.

Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, `CountProcessingQueueRecords`, `GetProcessingRecordDetail`, and `UpdateProcessingRecord`.

Notes:
- Officers can edit only records assigned to their display name.
- Officers can move the workflow only from step `Đã phân công` onward and cannot return to `Mới tiếp nhận` or `Đang phân loại`.
- Admin can update all workflow statuses; Leader is read-only.
- Area filters use `ObservableCollection<AreaSelectionOption>`.
- When saving step 5 (`Đang chờ bổ sung tài liệu`/initial result) and one of the three forms is missing, the ViewModel offers to generate it from `doc\templates\*.docx`, saves it as an attachment, and refreshes details.
