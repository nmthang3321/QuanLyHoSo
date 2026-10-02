# Page - Processing details and update

Files: `RecordProcessingView.xaml`, `RecordProcessingViewModel.cs`, `RecordModels.cs`, `InitialResultDocumentDetails.cs`, `InitialResultDocumentGenerator.cs`, and `AppDataService.cs`.

Service methods: `GetProcessingRecordDetail` and `UpdateProcessingRecord`.

- `ProcessingRecordDetail` is in `RecordModels.cs`; navigation/back/sidebar state involves `ShellViewModel`.
- When the detail page is opened through a resubmission row (record-list classify icon), it loads the linked original record and shows an amber banner (`ResubmissionNote`/`HasResubmissionNote`): "Bạn đang xử lý trên hồ sơ gốc <code> thông qua hồ sơ gửi lại <code>."
- The progress strip presents the eight steps documented in `engineering/PROCESSING.md`. It omits the former leadership-approval step and places `Chuyển cơ quan khác` before `Chờ kết quả`.
- Officer edits only assigned records and cannot choose the intake or classification states. Admin is unrestricted; Leader is read-only.
- The update card has no `Ghi chú` field: `Nội dung xử lý` starts blank on every open (the old server placeholder prefill was removed) and is required; after each update the refreshed history always shows that content under the current step. The record's stored `Note` is preserved server-side when the update request carries an empty note.
- `Người xử lý` is locked to the signed-in officer (`AuthContext.CurrentDisplayName`, combo disabled via `CanChangeProcessor`); only Admin can change it. Leaders remain read-only through `CanUpdateProcessing`.
- Choosing and saving `Chuyển cơ quan khác` requires the destination agency, records the transfer milestone, and automatically advances the current status to `Chờ kết quả`.
- The `Chuyển cơ quan khác` history milestone records the destination agency as `Chuyển đến: <agency>.`. Re-transferring updates the existing milestone (date, processor, and agency) instead of leaving the first transfer's entry. History milestones saved before this change display the record's current destination agency through a read-time fallback.
- Attachment paths are server-owned. If a restored or moved database still contains an absolute path from an older Server data root (for example `C:\ProgramData\QuanLyHoSo\...`), opening the attachment resolves the same record/file under the current `QuanLyHoSoFiles\Attachments` root and repairs the stored path. Server initialization and full-package restore also rebase these stale paths in bulk when the packaged physical file is present.
- LAN attachment downloads support Vietnamese filenames through an ASCII-safe `Content-Disposition` fallback and UTF-8 `filename*`; filenames such as `807_Thái Quốc Quân_Lưu đơn, trả lời X05_0001.pdf` must not cause `HttpListener` header validation failures.
- The related-document area uses a full-width vertical layout: the file list grows with its current items up to a scrollable maximum height, and the full-width drag-and-drop area remains fixed at the bottom.
- The save-edited-copy action is available only for Word attachments (`.doc`, `.docx`); PDF and image attachments remain view/download-only.
- `Cơ quan chuyển đến` uses the same overlay/group/search interaction as the intake area selector, not a flat ComboBox.
- At `Kết quả xử lý ban đầu`, missing generated forms can be created from `doc\templates\`, appended, and refreshed. Detection is case-insensitive and supports timestamp suffixes.
- Generating step-5 documents also persists `TransferDocumentNumber`, `TransferDocumentDate`, `CommanderApproverName`, and `LeaderApproverName` on the record so the record list can expose them as optional columns.
- The generation popup contains required multiline `Nhận xét` and `Đề xuất` editors plus required `Tên đội trưởng` and `Tên lãnh đạo duyệt` inputs. Review/proposal always open blank and are not populated from database notes. The current record processor supplies `Cán bộ đề xuất`.
- Generated Word content preserves line breaks from the two multiline editors.
- `LoadProcessingAttachments` treats the record form as the attachment fallback when a processing-detail payload is empty, then synchronizes both `SelectedProcessingDetail.Attachments` and the observable UI collection.
- After loading `Attachments`, notify `HasAttachments`; otherwise persisted attachments remain visually collapsed.
- Attachment open actions download authenticated content from the server into a local cache when the stored path is not directly usable on the client.
- `Quay lại`, `Hủy xử lý`, and `Cập nhật` live in a fixed action bar below the scrollable detail content (same pattern as the record-input action bar), so they remain visible while scrolling. The former header back button and the in-card button pair were removed to avoid duplicates. `Hủy xử lý` and `Cập nhật` stay disabled when `CanUpdateProcessing` is false (read-only Leader view).
