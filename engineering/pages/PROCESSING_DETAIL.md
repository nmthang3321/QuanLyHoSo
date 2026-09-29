# Page - Processing details and update

Files: `RecordProcessingView.xaml`, `RecordProcessingViewModel.cs`, `RecordModels.cs`, `InitialResultDocumentDetails.cs`, `InitialResultDocumentGenerator.cs`, and `AppDataService.cs`.

Service methods: `GetProcessingRecordDetail` and `UpdateProcessingRecord`.

- `ProcessingRecordDetail` is in `RecordModels.cs`; navigation/back/sidebar state involves `ShellViewModel`.
- The progress strip presents the eight steps documented in `engineering/PROCESSING.md`. It omits the former leadership-approval step and places `Chuyển cơ quan khác` before `Chờ kết quả`.
- Officer edits only assigned records and cannot choose the intake or classification states. Admin is unrestricted; Leader is read-only.
- Choosing and saving `Chuyển cơ quan khác` requires the destination agency, records the transfer milestone, and automatically advances the current status to `Chờ kết quả`.
- `Cơ quan chuyển đến` uses the same overlay/group/search interaction as the intake area selector, not a flat ComboBox.
- At `Kết quả xử lý ban đầu`, missing generated forms can be created from `doc\templates\`, appended, and refreshed. Detection is case-insensitive and supports timestamp suffixes.
- The generation popup contains required multiline `Nhận xét` and `Đề xuất` editors plus required `Tên đội trưởng` and `Tên lãnh đạo duyệt` inputs. Review/proposal always open blank and are not populated from database notes. The current record processor supplies `Cán bộ đề xuất`.
- Generated Word content preserves line breaks from the two multiline editors.
- `LoadProcessingAttachments` treats the record form as the attachment fallback when a processing-detail payload is empty, then synchronizes both `SelectedProcessingDetail.Attachments` and the observable UI collection.
- After loading `Attachments`, notify `HasAttachments`; otherwise persisted attachments remain visually collapsed.
- Attachment open actions download authenticated content from the server into a local cache when the stored path is not directly usable on the client.
