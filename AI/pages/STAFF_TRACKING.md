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
- Khoi thong bao theo role: `Leader` co form gui thong bao, `Officer` doc thong bao moi nhat danh cho minh, `Admin` doc thong bao moi nhat tu lanh dao.
- Bar chart dung `StaffBarStat`.
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
