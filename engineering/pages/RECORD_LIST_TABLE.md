# Page - Record-list table

Files: record-list XAML/code-behind and list/row ViewModels.

- DataGrid binds `Records`, uses light horizontal/vertical grid lines, and adjusts its maximum height to the number of rows currently loaded (`38 + row count × 34`). With short result sets the table and its card contract vertically; with longer sets the table remains constrained by the available page height and scrolls internally.
- Optional columns (via "Cột hiển thị") include the step-5 document metadata `Chỉ huy duyệt`, `Lãnh đạo duyệt`, `Phiếu chuyển đơn số`, `Ngày chuyển đơn` (populated when step-5 documents are generated), `Đơn vị chuyển đến` (the record's `AreaName` once the record reaches step 6+), and `Kết quả của đơn vị` (the step-8 `Lưu hồ sơ` history content of resolved records). The six new columns are hidden by default and must be enabled in "Cột hiển thị"; all six are also exported to Excel when visible.
- Resubmission rows (`Đã giải quyết — hồ sơ gửi lại`) show the classify action; it redirects to the linked original record and the processing page displays the "đang xử lý trên hồ sơ gốc" banner. The row tooltip says "Phân loại / xử lý trên hồ sơ gốc" (leaders keep the read-only wording).
- The status chip of resubmission rows displays `Hồ sơ gửi lại` (`StatusDisplay`) with its own magenta palette (#B42467 on #FCE3EF) instead of the resolved green; the chip color no longer mirrors the resolved status. The stored status, clipboard content, filters, and exports keep the full `Đã giải quyết — hồ sơ gửi lại` value.
- See record-detail and Excel-export feature docs.
- Row edit permission is `AuthContext.CanEditRecord(record.ProcessorName)`; it no longer checks client mode.
- Admin bulk-selection mode reveals the checkbox column and supports current page, all filtered results, clear selection, and delete selected. Disabling the mode clears selections.
- Selection is keyed by `RecordCode` across pages. Filter/reload/cancel clears it; paging preserves it.
- Select-all and delete run in the background and lock content. Delete confirmation defaults to No and reports success/failure before paging reload.
- Export uses separate `GetExportColumns` and never exports selection state.
- Delete is soft delete; restoration is through the Trash button. See `engineering/features/RECORD_TRASH.md`.
