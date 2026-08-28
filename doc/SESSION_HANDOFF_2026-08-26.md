# Handoff Session 2026-08-26

File này ghi lại trạng thái làm việc hiện tại để mở session mới có thể tiếp tục nhanh, không phải đọc lại toàn bộ chat.

## Bối cảnh dự án

- Dự án: `QuanLyHoSo`
- Loại app: WPF desktop app, C#, `.NET 5.0`
- Workspace: `D:\PROJECT\QuanLyHoSo`
- Mục tiêu đang làm: dựng giao diện draft 5 page, sau đó nối dữ liệu SQLite thay cho dữ liệu hard-code, seed dữ liệu mẫu và tinh chỉnh dashboard.

## Commit đã có

- `3bf901d Add system design documentation`
  - Thêm tài liệu thiết kế hệ thống WPF/.NET 5.
- `388a67e Draft UI design for records app`
  - Thêm UI draft 5 page, MVVM cơ bản, `.gitignore` ban đầu.

## Trạng thái Git hiện tại

Hiện có nhiều thay đổi **chưa commit**. Kết quả `git status --short` gần nhất:

```text
 M .gitignore
 M App.xaml
 M Models/RecordModels.cs
 M QuanLyHoSo.csproj
D  QuanLyHoSo.csproj.user
 M ViewModels/DashboardViewModel.cs
 M ViewModels/ExportViewModel.cs
 M ViewModels/RecordInputViewModel.cs
 M ViewModels/RecordProcessingViewModel.cs
 M ViewModels/SettingsViewModel.cs
 M ViewModels/ShellViewModel.cs
 M Views/Dashboard/DashboardView.xaml
 M Views/Dashboard/DashboardView.xaml.cs
 M Views/Export/ExportView.xaml
 M Views/Records/RecordInputView.xaml
 M Views/Records/RecordProcessingView.xaml
 M Views/Settings/SettingsView.xaml
?? Infrastructure/
```

`QuanLyHoSo.csproj.user` đã được đưa ra khỏi Git index và nên để ignored/local, không commit lại.

## Những phần đã làm trong session này

### 1. Git ignore

- Sửa `.gitignore` để bỏ qua output build, `.vs`, `.vscode` local settings, Rider, coverage, logs và local data.
- Lưu ý quan trọng: đã chỉnh pattern `Data/` thành `/Data/` để không ignore nhầm thư mục source `Infrastructure/Data`.

### 2. Kết nối dữ liệu SQLite

- Thêm package vào `QuanLyHoSo.csproj`:

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="5.0.17" />
```

- Thêm service chính: `Infrastructure/Data/AppDataService.cs`
- DB local được tạo tại:

```text
%LocalAppData%\QuanLyHoSo\Data\quanlyhoso.db
```

- Service hiện tạo schema và seed dữ liệu:
  - `Areas`
  - `CatalogItems`
  - `Records`
  - `RecordAttachments`
  - `ProcessHistories`

- Đã seed:
  - 102 đơn vị hành chính An Giang theo danh sách user đưa: 85 xã, 14 phường, 3 đặc khu.
  - 50 hồ sơ mẫu ngẫu nhiên.
  - Dữ liệu danh mục cho nguồn tiếp nhận, loại vụ việc, lĩnh vực, nhóm nội dung, mức độ ưu tiên, hướng xử lý.
  - File đính kèm và lịch sử xử lý mẫu.

### 3. Bỏ dữ liệu hard-code trên UI chính

Các ViewModel đã chuyển sang đọc dữ liệu từ `AppDataService`:

- `DashboardViewModel`
- `RecordInputViewModel`
- `RecordProcessingViewModel`
- `ExportViewModel`
- `SettingsViewModel`
- `ShellViewModel`

`ShellViewModel` gọi `AppDataService.Instance.Initialize()` để đảm bảo DB/schema/data sẵn sàng trước khi tạo các page ViewModel.

### 4. Model/DTO đã bổ sung

Trong `Models/RecordModels.cs` đã thêm/cập nhật:

- `RecordFormDraft`
- `ProcessingRecordDetail`
- `ProcessStep.IconGlyph`

Các model dashboard nằm trong `Models/DashboardModels.cs`.

### 5. Dashboard đã chỉnh gần nhất

File chính:

- `Views/Dashboard/DashboardView.xaml`
- `Views/Dashboard/DashboardView.xaml.cs`
- `ViewModels/DashboardViewModel.cs`
- `Infrastructure/Data/AppDataService.cs`

Đã làm:

- Bộ lọc thời gian trên dashboard dùng dropdown preset:
  - `Tuần này`
  - `Tháng này`
  - `Năm này`
  - `Khác`
- Thanh filter hiển thị dạng giống draft GUI:

```text
Tháng này (01/08/2026 - 31/08/2026)
```

- Với `Tuần này / Tháng này / Năm này`, app tự tính khoảng ngày và reload dữ liệu.
- Với `Khác`, app mở `Popup` chứa 2 control `Calendar`:
  - `Ngày bắt đầu`
  - `Ngày kết thúc`
- Không còn hiển thị 2 ô `DatePicker` trực tiếp trên thanh filter.
- Có nút `Áp dụng` trong popup để reload dashboard theo khoảng ngày custom.
- Query dashboard lọc theo `Records.ReceivedDate`.
- Bảng `HỒ SƠ CẬP NHẬT GẦN ĐÂY`:
  - `IsReadOnly="True"`
  - lấy 10 hồ sơ bằng `_dataService.GetRecentRecords(10, FromDate, ToDate)`
  - `DataGrid.Height="378"` để hiển thị tối thiểu 10 dòng.

### 6. Style/UI đã có

Trong `App.xaml`:

- Có resource màu chung.
- Style chung cho:
  - `TextBlock`
  - `Button`
  - `TextBox`
  - `ComboBox`
  - `DatePicker`
  - `DataGrid`
  - `DataGridColumnHeader`
- Đã thêm:

```xml
<BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
```

Status badge đang dùng `StatusToBrushConverter`.

### 7. Nội dung tiếng Việt

Yêu cầu của user: toàn bộ nội dung hiển thị nên là tiếng Việt có dấu.

Lưu ý: khi xem output PowerShell trong một số lần đọc file, tiếng Việt có thể hiện mojibake do encoding terminal, nhưng file/app vẫn đang dùng tiếng Việt.

## Cách chạy/build

Build:

```powershell
dotnet build QuanLyHoSo.sln
```

Chạy app:

```powershell
dotnet run --project QuanLyHoSo.csproj
```

Nếu build báo lỗi không copy được `QuanLyHoSo.exe`, thường là do app đang mở và khóa file output. Đóng app hoặc kill đúng process `QuanLyHoSo` rồi build lại.

## Kết quả kiểm tra gần nhất

Đã chạy:

```powershell
dotnet build QuanLyHoSo.sln
```

Kết quả: build thành công.

Warning còn lại:

- `NETSDK1138`: target framework `net5.0-windows` đã hết support.

Không có lỗi compile sau các chỉnh sửa gần nhất.

## Lưu ý kỹ thuật cho session sau

- Không dùng dữ liệu hard-code cho dashboard/record/export nữa nếu có thể lấy từ `AppDataService`.
- Khi thêm logic dữ liệu, ưu tiên mở rộng `AppDataService` hoặc tách repository/service có lớp lang, tránh nhồi logic SQL vào ViewModel.
- ViewModel giữ vai trò state + command + bindable collection.
- Code-behind chỉ nên xử lý behavior thuần UI. Hiện `DashboardView.xaml.cs` chỉ mở popup lịch khi combobox đang ở mode `Khác`.
- Tránh commit file local/user-specific như `*.csproj.user`, `.vs/`, `bin/`, `obj/`.
- User có nhiều Git account, không đụng global Git config.
- Nếu cần commit, nên review `git diff` trước vì có nhiều thay đổi chưa commit từ các bước trước.

## Việc nên làm tiếp

- Chạy app trực tiếp để nhìn lại căn chỉnh dashboard sau thay đổi popup lịch.
- Nếu layout filter vẫn chưa giống mockup, chỉnh tiếp width/height của `DateFilterHost`, `ComboBox` và `Popup`.
- Cân nhắc thêm paging cho bảng recent nếu sau này cần xem hơn 10 dòng.
- Sau khi user đồng ý, commit nhóm thay đổi SQLite + dashboard filter + ignore vào một commit rõ nghĩa.

## Cập nhật bổ sung trong session hiện tại

Các thay đổi mới nhất sau phần handoff ban đầu:

### 1. Sửa biểu đồ trạng thái trên Dashboard

- Vòng tròn trong khối `HỒ SƠ THEO TRẠNG THÁI` không còn là vòng màu tĩnh gây hiểu nhầm toàn bộ hồ sơ đã giải quyết.
- Đã chuyển sang donut chart thật, mỗi trạng thái chiếm một lát theo tỷ lệ dữ liệu hiện tại.
- Ví dụ: tổng 10 hồ sơ, 5 `Đã giải quyết` thì lát xanh lá chiếm 50% vòng; các trạng thái còn lại chiếm phần tương ứng.
- File liên quan:
  - `Models/DashboardModels.cs`: thêm `StatusStat.Width`, `StatusStat.StartAngle`, `StatusStat.SweepAngle`.
  - `Infrastructure/Data/AppDataService.cs`: `GetStatusStats(...)` tính thêm chiều rộng thanh tỷ lệ và góc donut.
  - `Presentation/Converters/StatusDonutSegmentConverter.cs`: converter mới để vẽ từng cung donut.
  - `App.xaml`: đăng ký resource `StatusDonutSegmentConverter`.
  - `Views/Dashboard/DashboardView.xaml`: thay vòng tròn tĩnh bằng `ItemsControl` + `Path` vẽ từng lát màu theo `StatusStats`.
- Lỗi runtime đã gặp và đã sửa:
  - `PenLineCap` không nhận giá trị `Butt` trong WPF.
  - Đã đổi sang `StrokeStartLineCap="Flat"` và `StrokeEndLineCap="Flat"`.

### 2. Bảng `HỒ SƠ CẬP NHẬT GẦN ĐÂY`

- Yêu cầu mới nhất của user: bảng không dùng scroll để xem nhiều dòng, mà dùng phân trang.
- Bảng hiện mặc định tối đa 5 dòng/trang.
- Có ô nhập `Số dòng` để user chọn số dòng muốn hiển thị mỗi trang.
- Giới hạn số dòng/trang: tối thiểu `1`, tối đa `20`, mặc định `5`.
- Khi đổi số dòng/trang hoặc đổi bộ lọc ngày, dashboard tự quay về trang 1 và tính lại tổng số trang.
- File liên quan:
  - `ViewModels/DashboardViewModel.cs`: thêm `CurrentRecentPage`, `TotalRecentPages`, `RecentPageText`, `RecentRecordsPageSizeText`, `RecentTableHeight`, `PreviousRecentPageCommand`, `NextRecentPageCommand`.
  - `Infrastructure/Data/AppDataService.cs`: `GetRecentRecords(...)` hỗ trợ tham số `skip`, query dùng `LIMIT $take OFFSET $skip`.
  - `Views/Dashboard/DashboardView.xaml`: `DataGrid.Height` bind với `RecentTableHeight`, tắt scroll dọc, thêm footer nhập số dòng và phân trang.

### 3. Làm đẹp nút phân trang

- Thêm style cục bộ `PaginationButton` trong `Views/Dashboard/DashboardView.xaml`.
- Nút phân trang hiện là nút nhỏ `36x36`, bo góc 6px, icon căn giữa.
- Có hover/pressed/disabled state rõ ràng.
- Text `Trang x/y` nằm trong pill riêng giữa hai nút.

### 4. Chỉnh alignment Dashboard

- Cụm filter ngày phía trên Dashboard đã đổi từ `StackPanel` sang `Grid` với cột rõ ràng để icon, text ngày và dropdown thẳng hàng hơn.
- Dropdown preset ngày giữ width cố định `116`.
- Các cột trong bảng recent đổi sang width theo tỷ lệ `*` kết hợp `MinWidth`/`MaxWidth` để bảng lấp đầy chiều ngang card nhưng vẫn không resize vỡ layout.

### 5. Build/start gần nhất

- Đã build thành công bằng:

```powershell
dotnet build QuanLyHoSo.sln
```

- Khi app đang chạy khóa file exe, dùng build kiểm tra tạm:

```powershell
dotnet build QuanLyHoSo.sln -p:OutputPath=.verify-build\
```

- `.verify-build/` đã được thêm vào `.gitignore` vì đây chỉ là output build tạm.
- Warning còn lại: `NETSDK1138`, target framework `net5.0-windows` đã hết support.

### 6. Việc nên kiểm tra tiếp

- Mở app và kiểm tra lại trực quan Dashboard:
  - Donut chart có chia lát đúng theo tỷ lệ trạng thái.
  - Bảng recent mặc định 5 dòng.
  - Ô `Số dòng` đổi được số dòng và phân trang cập nhật đúng.
  - Nút trang trước/sau chỉ disable khi đang ở trang đầu/cuối.
  - Filter ngày và DataGrid đã thẳng hàng, không còn hụt về bên phải như ảnh user gửi.

## Cập nhật 2026-08-27

### 1. Commit đã tạo trong phiên trước

- Đã commit nhóm thay đổi lớn:

```text
f44eba2 Add record input and list workflows
```

- Commit này bao gồm phần nhập dữ liệu, danh sách hồ sơ, dashboard responsive, tài liệu đính kèm và các workflow liên quan trước khi bắt đầu chỉnh sâu trang phân loại xử lý.

### 2. Trang nhập dữ liệu hồ sơ

- Khi vào trang nhập dữ liệu lần đầu, form để trống, không tự đổ dữ liệu user/hồ sơ mẫu.
- Các label ở phần input đã in đậm hơn.
- Trường `Địa bàn xã/phường` có ô nhập tìm nhanh để lọc trong danh sách 102 địa bàn.
- Nút `Xóa`, `Lưu`, `Hủy bỏ` được đưa xuống cuối form.
- `Ngày tiếp nhận` dùng popup chọn ngày thiết kế lại cho gọn và đẹp hơn.
- Nút `Lưu` kiểm tra các field có dấu sao. Nếu thiếu sẽ hiện popup báo người nhập; đủ dữ liệu thì lưu database.
- Nút `Hủy bỏ` chỉ clean form, không thao tác database.
- Nút `Xóa` hiện popup xác nhận; nếu đồng ý thì xóa database, hiện popup đã xóa và clean form.

### 3. Tài liệu liên quan / đính kèm

- Phần tài liệu liên quan đã có thao tác chọn file từ File Explorer.
- Hỗ trợ kéo thả file vào toàn bộ vùng drop zone, không cần thả đúng vào nút `Chọn file`.
- Khi kéo file vào vùng drop zone, vùng này highlight để người dùng biết có thể thả.
- Khi chưa có tài liệu, chỉ hiển thị text căn giữa, bỏ icon và dòng mô tả định dạng.
- Khi có tài liệu, danh sách tài liệu hiển thị trong GUI nhập liệu theo dạng item đính kèm.
- Model `AttachmentDraft` có thêm `FilePath`; database `RecordAttachments` cũng lưu đường dẫn file.

### 4. Danh sách hồ sơ trong phần nhập dữ liệu

- Nút `Danh sách hồ sơ` mở trang danh sách riêng.
- Danh sách giống bảng ở trang tổng quan nhưng hiển thị nhiều hơn, tối đa 20 hồ sơ mới nhất.
- Có phân trang và chọn số dòng/trang tương tự dashboard.
- Mỗi hàng có nút icon:
  - `Xem`: mở popup chi tiết hồ sơ, nền phía sau bị xám và chỉ thao tác được trên popup.
  - `Chỉnh sửa`: quay về trang nhập dữ liệu và đổ thông tin hàng đó vào form.
  - `Xóa`: hiện popup xác nhận trước khi xóa.
- Trang danh sách có nút back về trang trước.
- Đã xử lý lỗi scroll chuột trong vùng bảng: khi rê chuột vào bảng vẫn scroll được danh sách/trang thay vì bị kẹt ở DataGrid.

### 5. Dashboard

- Khối `Hồ sơ theo trạng thái` đã chỉnh responsive cho màn hình nhỏ hơn để tránh legend bị cắt chữ và các cột số bị ép sát.
- Donut/legend dùng layout linh hoạt hơn, có giới hạn width và khoảng cách tốt hơn.

### 6. Trang phân loại xử lý - trang chính

- Trang `Phân loại & xử lý` hiện tại được thiết kế lại thành trang chính dạng danh sách hồ sơ cần xử lý, không mở ngay trang chi tiết.
- Có header, ô tìm kiếm, bộ lọc trạng thái/địa bàn/ưu tiên và các thẻ số liệu nhanh:
  - Cần phân loại
  - Đang xử lý
  - Chờ bổ sung
  - Quá hạn
- Bảng danh sách lấy tối đa 20 hồ sơ mới nhất đang cần xử lý từ database.
- Cột thao tác chỉ còn:
  - Nút `Xem`: mở popup chi tiết người/hồ sơ giống danh sách hồ sơ ở phần nhập dữ liệu.
  - Nút `Phân loại xử lý`: mở trang phụ xử lý hồ sơ.
- Đã bỏ nút `Chuyển xử lý`.

### 7. Trang phụ phân loại/xử lý theo GUI mẫu

- Trang phụ làm theo hình chỉ định:

```text
D:\PROJECT\QuanLyHoSo\doc\GUI\phan_loai_xu_ly.png
```

- Phần đầu hiển thị card tóm tắt hồ sơ đang xử lý: mã hồ sơ, trạng thái, ngày tiếp nhận, nguồn tiếp nhận, người gửi, điện thoại, địa bàn, loại vụ việc, lĩnh vực.
- Có nút quay lại danh sách hồ sơ.
- Có khu `QUY TRÌNH XỬ LÝ HỒ SƠ` gồm 7 bước:
  1. Tiếp nhận
  2. Phân loại
  3. Phân công
  4. Xác minh
  5. Gia hạn
  6. Kết thúc
  7. Lưu hồ sơ
- Đã bỏ số nhỏ trên icon timeline.
- Với bước chưa tới: vòng tròn màu xám, icon màu đen để nhìn rõ.
- Text bước hiển thị dạng `1. Tiếp nhận`; nếu bước đã qua có dấu tick xanh phía sau.
- Đã thêm đường nối ngang giữa các bước trong quy trình.
- Khu `LỊCH SỬ XỬ LÝ` hiển thị lịch sử theo chiều dọc và có đường nối giữa các bước.
- Khu `CẬP NHẬT XỬ LÝ` có form:
  - Trạng thái hiện tại
  - Ngày xử lý
  - Người xử lý
  - Nội dung xử lý
  - Ghi chú
  - Nút `Hủy xử lý`
  - Nút `Cập nhật`

### 8. Lưu lịch sử xử lý và nhảy bước quy trình

- `AppDataService` đã bổ sung:
  - `GetProcessingQueueMetrics()`
  - `GetProcessingQueueRecords(...)`
  - `GetProcessingRecordDetail(string recordCode = null)`
  - `UpdateProcessingRecord(...)`
- Khi cập nhật xử lý:
  - Cập nhật `Status`, `ProcessorName`, `Note`, `UpdatedAt` trong bảng `Records`.
  - Ghi thêm dòng lịch sử vào `ProcessHistories`.
  - Nếu hồ sơ nhảy sang bước sau mà các bước trước chưa có lịch sử, hệ thống tự tạo lịch sử cho các bước trước với cùng ngày xử lý hiện tại.
- `ProcessStep` đã thêm:
  - `DateText`
  - `HasPreviousStep`
  - `HasNextStep`
- `ProcessHistoryItem` đã thêm:
  - `IsCurrent`
- Lịch sử được sắp theo thứ tự quy trình cố định, không chỉ theo thời gian insert.

### 9. Kiểm tra build mới nhất

- Đã build kiểm tra bằng output tạm để tránh lỗi file exe bị app đang chạy khóa:

```powershell
dotnet build QuanLyHoSo.sln -p:OutputPath=.verify-build\
```

- Kết quả: build thành công.
- Warning còn lại: `NETSDK1138`, target framework `net5.0-windows` đã hết support.

### 10. Trạng thái Git sau cập nhật này

- Các file đang có thay đổi chưa commit liên quan trực tiếp đến phần phân loại/xử lý và các chỉnh giao diện gần đây:
  - `Infrastructure/Data/AppDataService.cs`
  - `Models/RecordModels.cs`
  - `ViewModels/RecordProcessingViewModel.cs`
  - `Views/Records/RecordProcessingView.xaml`
- File handoff này hiện vẫn là untracked nếu chưa được add vào Git:
  - `doc/SESSION_HANDOFF_2026-08-26.md`

## Cập nhật 2026-08-28

### 1. Commit đã tạo sau phần nhập liệu/phân loại

- Đã commit nhóm chỉnh giao diện nhập dữ liệu và phân loại/xử lý:

```text
d3ac157 Polish record input and processing views
```

- Commit này gồm:
  - Làm đẹp trang `Nhập dữ liệu`.
  - Chỉnh timeline và lịch sử xử lý của trang `Phân loại & xử lý`.
  - Thêm popup xem chi tiết hồ sơ trong trang xử lý.

### 2. Trang phân loại & xử lý

- Đã chỉnh `QUY TRÌNH XỬ LÝ HỒ SƠ`:
  - Đường nối ngang giữa các bước đã qua có màu xanh.
  - Đường nối các bước chưa tới có màu xám.
  - Bước hiện tại màu xanh dương.
  - Bước đã qua có badge tròn xanh chứa dấu tick trắng.
  - Số thứ tự trước tên bước cũng đổi màu theo trạng thái.
- Đã chỉnh `LỊCH SỬ XỬ LÝ`:
  - Không còn hiển thị như bảng có từng hàng bị chia cắt.
  - Marker là vòng tròn nhỏ có tick bên trong với bước đã qua/hiện tại.
  - Đường nối dọc xanh cho phần đã qua, xám cho phần chưa tới.
  - Bước chưa xử lý vẫn hiện text `Chưa thực hiện`.
  - Bước chưa xử lý không hiện dòng `Người xử lý` và nội dung xử lý.
- Khi người dùng cập nhật lùi trạng thái, ví dụ từ `Kết thúc` về `Phân công`:
  - App đặt lại bước hiện tại theo trạng thái mới.
  - Xóa lịch sử từ bước hiện tại mới trở về sau, rồi ghi lại lịch sử cho bước vừa cập nhật.
  - Các bước trước đó vẫn giữ nếu đã có lịch sử thật.
- Đã normalize trạng thái lịch sử khi đọc dữ liệu:
  - Mọi bước nhỏ hơn bước hiện tại được xem là đã hoàn thành để tránh timeline bị xanh/xám xen kẽ do dữ liệu cũ.
- File liên quan:
  - `Infrastructure/Data/AppDataService.cs`
  - `Models/RecordModels.cs`
  - `ViewModels/RecordProcessingViewModel.cs`
  - `Views/Records/RecordProcessingView.xaml`

### 3. Trang nhập dữ liệu

- Đã làm lại bố cục trang `Nhập dữ liệu` cho gọn và đồng bộ hơn với dashboard/phân loại:
  - Label đậm và rõ hơn.
  - Các card `Thông tin chung`, `Thông tin liên quan`, `Thông tin bổ sung` thoáng hơn.
  - Tăng chiều cao ô nội dung và ghi chú.
  - Khối `Tài liệu liên quan` gọn hơn, có icon info nhỏ ở tiêu đề.
  - Danh sách file đính kèm hiển thị dạng dòng/bullet nhẹ, không còn card dày từng file.
  - Nút xem/xóa tài liệu nhỏ gọn hơn.
  - Drop zone thấp hơn, cân hơn.
- Sau chỉnh sửa cuối, hàng nút `Xóa / Lưu / Hủy bỏ` đã được đưa xuống dưới cùng, nằm sau khối tài liệu liên quan.
- File liên quan:
  - `Views/Records/RecordInputView.xaml`

### 4. Trang xuất dữ liệu - bộ lọc

- Đã chỉnh card `CHỌN BỘ LỌC DỮ LIỆU`:
  - Bỏ filter `Thời gian tiếp nhận`.
  - Bỏ filter riêng `Xã / Phường`.
  - Gộp thành một filter `Địa bàn`.
  - `Đến ngày` nằm cạnh `Từ ngày`.
  - `Từ ngày` và `Đến ngày` dùng `DatePicker` có popup lịch, style giống ngày tiếp nhận.
- Layout filter hiện gồm:
  - Hàng 1: `Từ ngày`, `Đến ngày`, `Trạng thái hồ sơ`, `Loại vụ việc`, `Lĩnh vực`.
  - Hàng 2: `Địa bàn`, `Người xử lý`, `Từ khóa tìm kiếm`, `Sắp xếp theo`.
- Nút `Xem dữ liệu` đã lọc thật preview theo:
  - khoảng ngày,
  - trạng thái,
  - loại vụ việc,
  - lĩnh vực,
  - địa bàn,
  - người xử lý,
  - từ khóa,
  - sắp xếp.
- Nút `Đặt lại` reset filter về mặc định:
  - `Từ ngày`: ngày đầu tháng hiện tại.
  - `Đến ngày`: hôm nay.
  - Các combobox: `Tất cả`.
  - Từ khóa: rỗng.
  - Sắp xếp: `Ngày tiếp nhận mới nhất trước`.
- File liên quan:
  - `Views/Export/ExportView.xaml`
  - `ViewModels/ExportViewModel.cs`
  - `Infrastructure/Data/AppDataService.cs`

### 5. Trang xuất dữ liệu - chọn cột và định dạng

- Trong card `CHỌN ĐỊNH DẠNG XUẤT`:
  - Bỏ checkbox `Định dạng ngày dd/mm/yyyy`.
  - Bỏ checkbox `Chỉ xuất cột đang hiển thị`.
  - Thêm khu `Cột dữ liệu` với checkbox chọn từng cột:
    - `STT`
    - `Mã hồ sơ`
    - `Ngày tiếp nhận`
    - `Người gửi đơn`
    - `Địa bàn`
    - `Loại vụ việc`
    - `Lĩnh vực`
    - `Trạng thái`
- Checkbox cột có tác dụng ngay trên bảng xem trước:
  - Bỏ tick cột nào thì cột đó ẩn trong preview.
- Do `DataGridColumn` không nằm trong visual tree, đã thêm helper:

```text
Presentation/BindingProxy.cs
```

- `ExportView.xaml` dùng `ViewModelProxy` để bind `Visibility` của các cột.

### 6. Trang xuất dữ liệu - xuất file thật

- Nút `Xuất dữ liệu` đã hoạt động thật:
  - Lấy toàn bộ dữ liệu theo filter hiện tại.
  - Tôn trọng định dạng đang chọn: `Excel (.xlsx)` hoặc `CSV (.csv)`.
  - Tôn trọng các cột được đánh dấu trong khu `Cột dữ liệu`.
  - Tôn trọng checkbox `Xuất kèm tiêu đề cột`.
  - Tự sinh tên file dạng:

```text
QuanLyHoSo_yyyyMMdd_HHmmss.xlsx
QuanLyHoSo_yyyyMMdd_HHmmss.csv
```

  - Mở `SaveFileDialog` để người dùng chọn nơi lưu file.
- Xuất CSV:
  - Ghi file UTF-8 có BOM.
  - Escape giá trị theo CSV.
  - Cột `Ngày tiếp nhận` được ghi dạng `="dd/MM/yyyy"` để Excel không tự parse ngày lẫn lộn kiểu `14/08/2026` và `12/8/2026`.
- Xuất XLSX:
  - Tạo file `.xlsx` bằng OpenXML package tối giản qua `ZipArchive`, không thêm package ngoài.
  - Cột `STT` được ghi là number cell thật, tránh cảnh báo Excel `number stored as text`.
  - Các cột còn lại ghi dạng inline string.
- `AppDataService.GetExportPreview(...)` đã bỏ giới hạn cứng 200 dòng khi export; preview vẫn gọi mặc định 50 dòng.

### 7. Lỗi đã sửa

- Trang `Xuất dữ liệu` từng crash khi mở do `DataGridColumn.Visibility` bind tới `ViewModelProxy` nhưng thiếu resource trong `ExportView.xaml`.
- Đã thêm:

```xml
<presentation:BindingProxy x:Key="ViewModelProxy" Data="{Binding}" />
```

- Sau đó build thành công.

### 8. Build mới nhất

- Đã build kiểm tra nhiều lần bằng:

```powershell
dotnet build QuanLyHoSo.sln -p:OutputPath=.verify-build\
```

- Kết quả mới nhất: build thành công.
- Warning còn lại:
  - `NETSDK1138`: target framework `net5.0-windows` đã hết support.

### 9. Trạng thái Git hiện tại

Kết quả `git status --short` gần nhất:

```text
 M Infrastructure/Data/AppDataService.cs
 M ViewModels/ExportViewModel.cs
 M Views/Export/ExportView.xaml
?? Presentation/BindingProxy.cs
?? doc/SESSION_HANDOFF_2026-08-26.md
```

- Các thay đổi trang xuất dữ liệu và `BindingProxy.cs` hiện chưa commit.
- File handoff này vẫn untracked nếu chưa được add vào Git.

---

## 2026-08-28 - Performance instrumentation and UI polish

### 1. Performance instrumentation

- Đã thêm log đo thời gian cho các điểm tải dữ liệu chính:
  - `AppDataService.Initialize()`
  - `AppDataService.GetProcessingQueueRecords(...)`
  - `AppDataService.GetExportPreview(...)`
  - `AppDataService.CountExportRecords(...)`
  - `ShellViewModel` khi khởi tạo database và tạo các page ViewModel.
- Đã thêm `AppDataService.CountCatalogItemsByType()` để lấy số lượng item theo từng loại danh mục bằng một query thay vì gọi nhiều query lặp lại trong trang cài đặt.
- `SettingsViewModel.RefreshCatalogGroupCounts()` đã dùng batch count mới.
- Đã commit và push trước đó:

```text
3b7773a Add performance instrumentation and optimize catalog counts
```

### 2. Typography polish

- Đã thêm màu chữ `StrongTextColor` / `StrongTextBrush` trong `App.xaml`.
- Chữ đậm nhỏ trong app được chuyển sang xám đậm hơn để giảm cảm giác quá nặng.
- `PageTitleText` vẫn giữ `TextBrush` để title lớn còn rõ và đậm.
- `DataGridColumnHeader` vẫn giữ `TextBrush`, đúng yêu cầu header bảng vẫn đậm.
- `ExportFilterLabel` trên trang xuất dữ liệu dùng `StrongTextBrush`.
- `InputLabelText` trên trang nhập dữ liệu đổi từ `Bold` sang `SemiBold` và dùng `StrongTextBrush`.
- `PrimaryButton` đã ép `TextBlock` con dùng màu trắng để icon/text trong nút xanh nhất quán.
- Đã commit phần này:

```text
81cfe4b Refine bold text styling
```

### 3. UI consistency polish đang chuẩn bị commit

- Thêm style dùng chung:
  - `FieldLabelText`: label nhỏ màu xám.
  - `DetailValueText`: value quan trọng màu xám đậm vừa phải.
  - `SubsectionTitleText`: tiêu đề phụ trong khối chi tiết.
- Áp dụng các style này cho:
  - popup chi tiết hồ sơ trong `Views/Records/RecordListView.xaml`;
  - card chi tiết xử lý và popup chi tiết hồ sơ trong `Views/Records/RecordProcessingView.xaml`;
  - form `Cập nhật xử lý` trong `Views/Records/RecordProcessingView.xaml`.
- Sửa hover highlight bị kỳ trong trang cài đặt:
  - thêm `CatalogGroupButton` trong `Views/Settings/SettingsView.xaml`;
  - bỏ hover background mặc định của WPF `Button` quanh card danh mục;
  - giữ nguyên trạng thái selected bằng `CatalogGroupBorder`.

### 4. Verification mới nhất

- Đã build kiểm tra bằng:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build
```

- Kết quả: build thành công, `0 errors`.
- Warning còn lại:
  - `NETSDK1138`: target framework `net5.0-windows` đã hết support.
- Build mặc định có thể fail nếu app `QuanLyHoSo.exe` đang mở vì file output trong `bin/Debug/net5.0-windows` bị khóa.

### 5. Git / remote

- Remote hiện tại:

```text
origin https://github.com/nmthang3321/QuanLyHoSo.git
```

- File handoff này được cập nhật để commit cùng phase UI polish.
