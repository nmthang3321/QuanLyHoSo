# Page - Record-list table

Files: record-list XAML/code-behind and list/row ViewModels.

- DataGrid binds `Records`, uses light horizontal/vertical grid lines, binds height to `TableHeight`, and forwards mouse wheel to `RecordListScrollViewer`.
- See record-detail and Excel-export feature docs.
- Row edit permission is `AuthContext.CanEditRecord(record.ProcessorName)`; it no longer checks client mode.
- Admin bulk-selection mode reveals the checkbox column and supports current page, all filtered results, clear selection, and delete selected. Disabling the mode clears selections.
- Selection is keyed by `RecordCode` across pages. Filter/reload/cancel clears it; paging preserves it.
- Select-all and delete run in the background and lock content. Delete confirmation defaults to No and reports success/failure before paging reload.
- Export uses separate `GetExportColumns` and never exports selection state.
- Delete is soft delete; restoration is through the Trash button. See `AI/features/RECORD_TRASH.md`.
