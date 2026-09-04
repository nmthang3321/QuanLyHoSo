# Theo doi can bo

Chi tiet theo trang:
- `AI/pages/STAFF_TRACKING.md`

File can mo:
- `ViewModels\StaffTrackingViewModel.cs`
- `Views\Records\StaffTrackingView.xaml`
- `Views\Records\StaffTrackingView.xaml.cs`
- `Models\StaffTrackingModels.cs`
- `ViewModels\ShellViewModel.cs`
- `App.xaml`

Trang thai hien tai:
- Da co UI trang "Theo doi can bo" va menu sidebar moi.
- Sidebar van dung style/chung layout trong `MainWindow.xaml`.
- `Officer` chi hien thi thong tin cua can bo dang dang nhap (`AuthContext.CurrentDisplayName`).
- Du lieu bang/metric/deadline da noi tu `AppDataService`.
- Da them LAN endpoint cho bang/metric/deadline/ho so dang xu ly va thong bao lanh dao.

Thanh phan UI chinh:
- Header + nut "Xuat bao cao".
- Bo loc ky bao cao, phong/bo phan, can bo, trang thai.
- 4 metric cards: Tong ho so, Dang xu ly, Sap qua han, Da qua han.
- Bang hieu suat can bo.
- Panel thong tin can bo ben phai.
- Danh sach ho so dang xu ly cua can bo dang chon.
- Panel theo role: `Admin` co card gui thong bao; `Leader` co card gui thong bao va card dat KPI rieng; `Officer` nhan thong bao.
- Thong bao hien theo inbox: moi nhat o tren cung, 5 thong bao/trang, chua doc in dam va co badge do unread o sidebar muc Theo doi can bo.
- KPI cua lanh dao luu rieng trong bang `LeadershipKpiTargets`, khong tron vao inbox thong bao.
- Bieu do cot hieu suat va donut tinh trang deadline.

Ghi chu tiep theo:
- Khi noi data that, uu tien them model/service methods trong `AppDataService`.
- Neu can ho tro client/server LAN, them request/route trong `Infrastructure\Network\LanApiModels.cs` va `Infrastructure\Network\LanDataServer.cs`.
- Can can nhac role: Admin/Leader xem tong hop, Officer chi xem ho so/can bo dung ten minh neu ap dung scope hien co.
