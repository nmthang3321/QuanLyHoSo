# Classification and processing

Detailed references: `engineering/pages/PROCESSING_QUEUE.md` and `engineering/pages/PROCESSING_DETAIL.md`.

Open `ViewModels\RecordProcessingViewModel.cs` and `Views\Records\RecordProcessingView.xaml`.

Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, `CountProcessingQueueRecords`, `GetProcessingRecordDetail`, and `UpdateProcessingRecord`.

The displayed workflow has exactly eight ordered steps:

1. `Tiếp nhận`
2. `Phân loại`
3. `Phân công`
4. `Xác minh`
5. `Kết quả xử lý ban đầu`
6. `Chuyển cơ quan khác`
7. `Chờ kết quả`
8. `Lưu hồ sơ`

Notes:

- Officers can edit only records assigned to their display name and cannot return to the intake/classification states. Admin can update all workflow statuses; Leader is read-only.
- The former `Đang chờ bổ sung tài liệu` value is normalized to `Kết quả xử lý ban đầu`. `Đã giải quyết` is displayed as the final `Lưu hồ sơ` step.
- Saving `Chuyển cơ quan khác` records that step as completed and immediately persists the current status as `Chờ kết quả`. The client includes a compatibility fallback for older servers.
- At `Kết quả xử lý ban đầu`, missing forms can be generated from `doc\templates\*.docx`, stored on the server, attached to the record, and then refreshed in the detail view.
- The initial-result popup starts `Nhận xét` and `Đề xuất` empty. Both are required multiline fields and do not read the record's stored note fields.
- The popup also requires `Tên đội trưởng` and `Tên lãnh đạo duyệt`. The proposing officer is taken from the record's current `Người xử lý`.
- In `phieu_de_xuat.docx`, the entered team leader replaces `Khưu Quốc Hiếu`, the current processor replaces `Quách Văn Bền`, and the entered leader name replaces the whole static `Thượng tá Đặng Văn Thinh` string.
