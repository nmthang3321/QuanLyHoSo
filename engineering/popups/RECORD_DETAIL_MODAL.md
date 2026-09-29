# Popup - Record detail

Files: record-list XAML/code-behind/ViewModel, record models, and `AppDataService`.

Service method: `GetRecordForm`.

The modal is an overlay at the end of the root `RecordListView.xaml` Grid. Close binds `CloseDetailCommand`; attachments come from `SelectedRecordDetail.Attachments`.

## Linked records group ("Danh sách hồ sơ gửi lại")

- The card is the last block in the popup, after attachments, and is visible only when the record belongs to a resubmission group: `RecordFormDraft.HasLinkedRecords` (`SenderHistory.Count > 1` or a non-empty `ResubmissionReason`, the latter keeping a resubmission's reason visible even if its original is trashed). Records without links show no linked-records card at all.
- The current record's status chip sits in the popup header next to the record code, so status stays visible for every record.
- Each sender-history row renders through the `LinkedRecordExpanderTemplate` ControlTemplate in `RecordListView.xaml` resources: a collapsed header with the record code, a `Hồ sơ gốc`/`Gửi lại <code>` tag, and the colored status chip (`StatusToBrushConverter`).
- Status text everywhere in the popup uses the shared `RecordStatusDisplay.GetDisplay` mapping, so the stored `Đã giải quyết — hồ sơ gửi lại` value renders as the short `Hồ sơ gửi lại` chip, matching the record list. The converter still receives the raw stored status for colors.
- Clicking the header toggles the row. The expanded panel shows receive date and case type for every row. Resubmission rows additionally show `Lý do gửi lại: …` (per-row `SenderRecordHistory.ResubmissionReason`, now loaded by `GetSenderRecords`) and do not show the processing-history block. Original rows show the full processing history (`ResolutionSummary`) with a `Chưa có lịch sử xử lý.` placeholder when empty. Every row keeps a `Xem chi tiết hồ sơ này` button bound to `OpenSenderRecordCommand`.
- The layout is view-only: no database schema, service request, or LAN behavior change; `GetSenderRecords` additionally selects the existing `ResubmissionReason` column (additive JSON, safe across client/server versions), and `StatusDisplay`/`HasLinkedRecords`/`IsResubmission`/`HasResubmissionReason` are get-only computed properties following the existing `Relationship` pattern.
