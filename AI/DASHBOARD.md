# Dashboard

Detailed references: `AI/pages/DASHBOARD_OVERVIEW.md` and `AI/popups/DASHBOARD_DATE_RANGE.md`.

Open `ViewModels\DashboardViewModel.cs`, `Views\Dashboard\DashboardView.xaml[.cs]`, and `Models\DashboardModels.cs`.

Service methods: `GetDashboardMetrics`, `GetStatusStats`, `GetTopAreas`, `GetReceivedTrendStats`, `GetRecentRecords`, and `CountRecords`.

Notes:
- A previous not-responding defect caused by `CalculateNiceAxisStep` returning zero has been fixed.
- Dashboard reload runs asynchronously in the background.
