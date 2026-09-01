# Page - Dashboard overview

Dung khi task lien quan trang Tong quan/Dashboard.

Files:
- `Views\Dashboard\DashboardView.xaml`
- `Views\Dashboard\DashboardView.xaml.cs`
- `ViewModels\DashboardViewModel.cs`
- `Models\DashboardModels.cs`

Service methods:
- `GetDashboardMetrics`
- `GetStatusStats`
- `GetTopAreas`
- `GetReceivedTrendStats`
- `GetRecentRecords`
- `CountRecords`

Notes:
- Reload dashboard async/background.
- Da fix `CalculateNiceAxisStep` tra 0 gay not responding.
- Date filter menu nam trong code-behind `DashboardView.xaml.cs`.

