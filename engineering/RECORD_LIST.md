# Record list

Detailed references: `engineering/pages/RECORD_LIST_FILTERS.md`, `engineering/pages/RECORD_LIST_TABLE.md`, `engineering/popups/RECORD_DETAIL_MODAL.md`, `engineering/features/AREA_SELECTOR.md`, and `engineering/features/EXPORT_EXCEL.md`.

Open `ViewModels\RecordListViewModel.cs`, `ViewModels\RecordListRowViewModel.cs`, `Views\Records\RecordListView.xaml[.cs]`, and `Models\AreaSelectionModels.cs` for area work.

Service methods: `GetFilteredRecords`, `CountFilteredRecords`, `GetExportPreview`, `DeleteRecord`, and `GetRecordForm`.

Notes:
- Excel export belongs to this page; the separate Export page was removed.
- The area filter uses root overlay `AreaFilterOverlayCanvas` with search and expandable groups.
- Users can choose `Tất cả`, a group, or a child item; clicking a group applies it and toggles child visibility.
- Per-row edit visibility uses `AuthContext.CanEditRecord(record.ProcessorName)` and is not blocked by client mode. Client-mode edit/save goes through the LAN API.
