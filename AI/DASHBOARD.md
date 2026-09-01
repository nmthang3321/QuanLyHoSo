# Dashboard

Chi tiet theo trang/popup:
- `AI/pages/DASHBOARD_OVERVIEW.md`
- `AI/popups/DASHBOARD_DATE_RANGE.md`

File can mo:
- `ViewModels\DashboardViewModel.cs`
- `Views\Dashboard\DashboardView.xaml`
- `Views\Dashboard\DashboardView.xaml.cs`
- `Models\DashboardModels.cs`

Service methods:
- `GetDashboardMetrics`
- `GetStatusStats`
- `GetTopAreas`
- `GetReceivedTrendStats`
- `GetRecentRecords`
- `CountRecords`

Ghi chu:
- Da fix not responding do `CalculateNiceAxisStep` tra 0.
- Reload dashboard dang async/background.
