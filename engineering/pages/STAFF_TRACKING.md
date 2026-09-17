# Page - Staff tracking

Files: `StaffTrackingView.xaml[.cs]`, `StaffTrackingViewModel.cs`, and `StaffTrackingModels.cs`. Navigation uses the `StaffTracking` key in `ShellViewModel`; reuse the shared sidebar.

- UI contains period/department/officer/status filters, metric cards, a five-row paginated staff table, selected-staff panel, active records, role-specific notices/KPI controls, performance bars, and deadline donut.
- Officer sees only `AuthContext.CurrentDisplayName`.
- Admin sends reminders. Leader has `Thông báo`/`Nhắc nhở`/`Đặt KPI` tabs; Officer receives notices.
- Leader inbox and badge count only Admin-sent messages. Saving Leader KPI requires Yes/No confirmation and sends a separate KPI notice to each affected officer, not back to Leader.
- Notices are newest first, unpaginated, scroll inside their card, bold when unread, and open a detail popup that marks them read. Shell polls every five seconds after login and stops on sign-out.
- Leadership KPI uses separate `leadership-kpi/*` routes and `LeadershipKpiTargets`.
- Bar chart uses all officers in the selected period and a 0-100% axis in 25% steps. Deadline donut uses `StatusStat` and `StatusDonutSegmentConverter`.
- Data comes from `AppDataService`; Officer queries are scoped. `FilterCommand`, `RefreshCommand`, and `ExportCommand` are placeholders; `SelectedStaff` defaults to the first row.

Build verification: `dotnet build QuanLyHoSo.csproj -o .verify-builds/current`.
