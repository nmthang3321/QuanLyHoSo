# Page - Dashboard overview

Files: `DashboardView.xaml[.cs]`, `DashboardViewModel.cs`, and `DashboardModels.cs`.

Service methods: `GetDashboardMetrics`, `GetStatusStats`, `GetTopAreas`, `GetReceivedTrendStats`, `GetRecentRecords`, and `CountRecords`.

- Reload runs asynchronously in the background.
- The `CalculateNiceAxisStep` zero-step hang has been fixed.
- Date-filter menu behavior lives in `DashboardView.xaml.cs`.
