# Staff tracking

Detailed page reference: `AI/pages/STAFF_TRACKING.md`.

Open `ViewModels\StaffTrackingViewModel.cs`, `Views\Records\StaffTrackingView.xaml[.cs]`, `Models\StaffTrackingModels.cs`, `ViewModels\ShellViewModel.cs`, and `App.xaml`.

Current behavior:
- The page and sidebar item use shared shell styles.
- Officers see only their own data (`AuthContext.CurrentDisplayName`).
- Table, metric, deadline, active-record, and leadership-notice data come from `AppDataService` through LAN endpoints where applicable.
- UI includes report filters, four metric cards, the staff performance table, selected-staff information, active records, role-specific notice/KPI panels, a performance bar chart, and a deadline-status donut.
- Notices are newest first, fully loaded, scroll inside the card, and are not paginated. Unread notices are bold and contribute to the red sidebar badge.
- Leadership KPI targets are stored separately in `LeadershipKpiTargets`, not in the notice inbox.

When extending this page, add models/service methods to `AppDataService` first. LAN support requires DTOs/routes in `LanApiModels.cs` and `LanDataServer.cs`. Preserve scope: Admin/Leader can see aggregate data; Officer remains limited to their own records and identity.
