# Page - Dashboard overview

Files: `DashboardView.xaml[.cs]`, `DashboardViewModel.cs`, and `DashboardModels.cs`.

Service methods: `GetDashboardMetrics`, `GetStatusStats`, `GetTopAreas`, `GetReceivedTrendStats`, `GetRecentRecords`, and `CountRecords`.

- Reload runs asynchronously in the background. Independent dashboard queries start concurrently to avoid accumulating one LAN round-trip per widget.
- `IsLoading` displays a blocking loading surface during the initial fetch instead of exposing empty charts and cards.
- The `CalculateNiceAxisStep` zero-step hang has been fixed.
- Date-filter menu behavior lives in `DashboardView.xaml.cs`.
