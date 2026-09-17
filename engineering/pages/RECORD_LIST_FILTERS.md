# Page - Record-list filters

Files: `RecordListView.xaml[.cs]`, `RecordListViewModel.cs`, and `AppDataService.cs`.

Service methods: `GetFilteredRecords`, `CountFilteredRecords`, `BuildExportWhere`, and `AddOptionalAreaFilter`.

Filters: from/to date, status, case type, field, area, processor, keyword, and sort.

- `Bộ lọc` binds `IsFilterPanelOpen`.
- `Xem dữ liệu` runs `ApplyFilterCommand`; `Đặt lại` runs `ResetFilterCommand`.
- See `engineering/features/AREA_SELECTOR.md` for the area overlay.
