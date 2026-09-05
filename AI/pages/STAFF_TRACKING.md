# Page - Theo doi can bo

Dung khi task lien quan trang theo doi tien do, deadline, KPI hoac hieu suat xu ly theo tung can bo.

Files:
- `Views\Records\StaffTrackingView.xaml`
- `Views\Records\StaffTrackingView.xaml.cs`
- `ViewModels\StaffTrackingViewModel.cs`
- `Models\StaffTrackingModels.cs`

Navigation:
- DataTemplate dang ky trong `App.xaml`.
- Sidebar item duoc them trong `ViewModels\ShellViewModel.cs` voi key `StaffTracking`.
- Sidebar style nam trong `MainWindow.xaml`; khong tao sidebar rieng cho trang nay.

UI hien co:
- Page title: `THEO DOI XU LY CAN BO`.
- Top filters: period, department, officer, status, filter button, refresh button.
- Metric cards dung `StaffTrackingMetric`.
- Bang can bo dung `StaffPerformanceRow`.
- Right panel bind `SelectedStaff`.
- Active record cards dung `StaffWorkRecord`.
- Neu dang nhap role `Officer`, trang chi hien thong tin cua `AuthContext.CurrentDisplayName`.
- Khoi theo role: `Admin` gui nhac nho; `Leader` dung ba tab `Thong bao`/`Nhac nho`/`Dat KPI` trong cung card; `Officer` nhan thong bao.
- Inbox cua `Leader` chi lay thong bao do user role `Admin` gui; tab inbox la tab mac dinh khi mo trang.
- Badge thong bao chua doc o sidebar hien cho ca `Officer` va `Leader`; badge cua `Leader` chi dem tin tu `Admin`.
- Luu KPI cua `Leader` co xac nhan Yes/No; khi xac nhan se gui rieng thong bao KPI den tung can bo trong pham vi ap dung, khong gui nguoc cho `Leader`.
- Bang can bo hien 5 dong moi trang cho moi role; danh sach dai dung phan trang va khong cuon ben trong bang.
- Danh sach thong bao cua can bo sap xep moi nhat truoc, 5 dong/trang, co nut trang truoc/sau va nut danh dau da doc.
- Thong bao chua doc in dam; sidebar item `StaffTracking` co badge do bang so thong bao chua doc.
- Card thong bao nam o hang cuoi cung voi hai card bieu do, co cung chieu cao. Noi dung dai chi hien tom tat; bam vao mot thong bao se mo popup xem day du va tu dong danh dau da doc.
- KPI dat boi lanh dao dung route/bang rieng `leadership-kpi/*` va `LeadershipKpiTargets`, khong tao item thong bao moi.
- Bar chart dung `StaffBarStat`.
- Bar chart luon dung toan bo danh sach can bo trong ky, khong thay doi theo trang hien tai cua bang.
- Deadline donut dung `StatusStat` va converter `StatusDonutSegmentConverter`.

Data hien tai:
- Bang/metric/deadline lay tu `AppDataService`; `Officer` duoc loc theo can bo dang dang nhap.
- `FilterCommand`, `RefreshCommand`, `ExportCommand` dang la placeholder.
- `SelectedStaff` mac dinh la dong dau tien.

Khi noi database:
- Lay danh sach can bo tu `AppDataService.GetProcessorNames()`.
- Thong ke tu bang `Records` theo `ProcessorName`, `Status`, `ExpectedResultDate`.
- Ho so dang xu ly nen loc `Status <> 'Da giai quyet'` va theo can bo dang chon.
- KPI/ty le dung han co the tinh tu `ExpectedResultDate` va trang thai hoan thanh.
- LAN da co route cho staff performance/deadline/active records va leadership notices trong `LanApiModels.cs`, `LanDataServer.cs`.

Verify:
- Build khuyen dung:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```
