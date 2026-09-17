# Page - Processing details and update

Files: `RecordProcessingView.xaml`, `RecordProcessingViewModel.cs`, `RecordModels.cs`, and `AppDataService.cs`.

Service methods: `GetProcessingRecordDetail` and `UpdateProcessingRecord`.

- `ProcessingRecordDetail` is in `RecordModels.cs`; navigation/back/sidebar state involves `ShellViewModel`.
- Officer edits only assigned records and cannot choose `Mới tiếp nhận` or `Đang phân loại`; after `Đã phân công`, steps 3-7 are available. Admin is unrestricted; Leader is read-only.
- Attachments support PDF/Word/images. At step 5, missing generated forms can be created from `doc\templates\`, appended, and details refreshed. Detection is case-insensitive and supports timestamp suffixes.
- `LoadProcessingAttachments` treats the record form as the attachment fallback when a processing-detail payload is empty, then synchronizes both `SelectedProcessingDetail.Attachments` and the observable UI collection. This protects the page from displaying “no attachments” when the record itself still contains files.
- After loading `Attachments`, notify `HasAttachments`; without it, persisted attachments remain visually collapsed.
- For `Chuyển cơ quan khác`, `Cơ quan chuyển đến` must use the same overlay/group/search display as the intake area selector, not a flat ComboBox.
