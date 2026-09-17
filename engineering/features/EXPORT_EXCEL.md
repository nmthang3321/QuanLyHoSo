# Feature - Excel export

Files: `Views\Records\RecordListView.xaml`, `ViewModels\RecordListViewModel.cs`, and `Infrastructure\Data\AppDataService.cs`.

Service methods: `GetExportPreview`, `BuildExportWhere`, and shared `GetFilteredRecords` filtering where appropriate.

- Export lives on the record-list page; the separate Export page was removed.
- Export runs asynchronously in the background to avoid blocking the UI.
- `_isExporting` prevents duplicate export clicks.
