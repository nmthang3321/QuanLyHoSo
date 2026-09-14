# Session handoff - QuanLyHoSo

Cap nhat: 2026-09-13

Kiểm tra tổng kết phiên 2026-09-14: Release solution build và `tests/Scripts/run-all.ps1 -IncludeUI` pass 80 ca (40 unit + 39 integration + 1 UI smoke) trước khi commit toàn bộ thay đổi.

File nay nam trong `AI/` de gom toan bo context cho AI vao mot cho. Khi bat dau analyse:

1. Doc `AI/INDEX.md`.
2. Chay `git status --short --branch`.
3. Chi doc file trong `AI/` dung voi trang/chuc nang dang lam.

Routing chinh:
- Routing day du: `AI/INDEX.md`
- Trang nho: `AI/pages/*.md`
- Popup/overlay: `AI/popups/*.md`
- Chuc nang dung chung: `AI/features/*.md`
- Infra/DB/build/LAN: `AI/infra/*.md`
- Automated tests: `AI/infra/TESTING.md`

## Điều chỉnh 2026-09-14 - Thông tin phiếu chuyển đơn

- Bước Kết quả xử lý ban đầu -> Cập nhật -> đồng ý tạo tài liệu: mở overlay nhập Phiếu chuyển đơn số, Ngày phiếu chuyển đơn, Chuyển đơn tố cáo đề ngày. Cả ba bắt buộc; Hủy giữ form, Xác nhận mới lưu/tạo phiếu còn thiếu, lỗi giữ popup.
- Popup thêm xem trước chỉ đọc: Đơn ghi tên (SenderName), Nguồn đơn (ReceiveSource), Tóm tắt nội dung (Content), Nhận xét (Note, dùng ProcessingNote nếu đang nhập ghi chú mới), Đề xuất (AdditionalNote). Tải từ hồ sơ qua service; nội dung dài cuộn riêng, nút Xác nhận/Hủy luôn hiện dưới popup. Đã render/xem ảnh popup và chạy lại 5 ca smoke pass.
- Phần xem trước bổ sung Địa chỉ liên hệ (ContactAddress) chỉ đọc, đúng địa chỉ người gửi sẽ được điền vào phiếu.
- `InitialResultDocumentDetails` truyền qua service/LAN tới generator. Ánh xạ vùng tô vàng của cả 3 mẫu đã sửa theo số phiếu và 2 ngày riêng; giữ mẫu gốc, kiểu chữ của run và dữ liệu hồ sơ khác.
- Client/server phải dùng bản mới cùng nhau. Không thay schema. Thông tin nhập chỉ dùng cho lần tạo tài liệu, chưa lưu riêng thành trường hồ sơ.
- Verify: Debug client/server build riêng và Release solution build pass; full noninteractive suite 79 ca pass (40 unit + 39 integration). Thêm 5 ca kiểm thử tài liệu/validation/LAN serialization; render và xem ảnh popup WPF bằng dữ liệu tổng hợp. Chưa click-through luồng có đăng nhập.

## Điều chỉnh 2026-09-14 - Tên bước 6

- Thêm tooltip hướng dẫn tiếng Việt cho cả 7 icon quy trình trong `RecordProcessingView.xaml`, theo StepNumber; mô tả thao tác và làm rõ bước 5 hiện dùng trạng thái Đang chờ bổ sung tài liệu.
- Tooltip chỉ gắn vào vòng tròn icon, không gắn vào Grid chứa dây nối; glyph không nhận hit-test để rê ngay trên hình vẫn hiện tooltip.
- Quy trình trong trang chi tiết phân loại & xử lý: bước 6 hiển thị `Chờ kết quả` thay cho `Kết thúc`, bằng XAML trigger theo StepNumber. Không thay khóa tiêu đề lịch sử đã lưu hay ánh xạ trạng thái.
- Sửa lỗi mã hóa nhãn bước 6 mất dấu, đặt font Segoe UI. Cơ quan chuyển đến có dấu `*` đỏ khi chọn Chuyển cơ quan khác; validation form hiện có chặn lưu nếu chưa chọn cơ quan.
- Verify sau sửa: Debug client build và Release solution build pass; 5 ca smoke pass.

## Điều chỉnh 2026-09-14 - Thanh nút form cố định

- `RecordInputView.xaml`: chuyển thanh Lưu/Hủy bỏ/Xóa ra ngoài vùng cuộn, cố định ở cạnh dưới trang; áp dụng cả nhập mới và cập nhật hồ sơ dùng chung form.
- Nội dung cuộn trong hàng riêng; overlay địa bàn và đối chiếu phủ cả hai hàng. Thanh nút bị vô hiệu hóa khi popup đối chiếu mở.
- Verify: Debug client/server build riêng, Release solution build và 5 ca smoke pass. Chưa kiểm tra bố cục trực tiếp trong ứng dụng.

## Điều chỉnh 2026-09-14 - Bắt buộc Thông tin chung

- Trang nhập dữ liệu: toàn bộ trường trong mục Thông tin chung có dấu `*` đỏ và được kiểm tra trước khi lưu. Bổ sung bắt buộc Số điện thoại, Địa chỉ liên hệ, Địa chỉ xảy ra vụ việc và kiểm tra số hồ sơ tự sinh.
- Giá trị rỗng/chỉ khoảng trắng bị chặn bằng thông báo liệt kê trường thiếu; áp dụng khi nhập mới và sửa trong form. Các mục khác giữ nguyên quy tắc hiện tại.
- Verify: Debug client/server build riêng, Release solution build và 5 ca smoke pass. Chưa kiểm tra thao tác form trực tiếp.

## Điều chỉnh 2026-09-14 - Chú thích card thống kê

- Lãnh đạo có icon Xem chi tiết xử lý (chỉ xem) ở bảng hồ sơ thường, dùng luồng mở từ danh sách hiện có. Form xử lý vẫn bị vô hiệu hóa theo CanUpdateProcessing, lệnh lưu và server vẫn chặn sửa của Leader. Thêm kiểm thử quyền icon/không sửa/không xóa; Release solution build và 6 ca smoke pass.
- Điều hướng đăng nhập trong ShellViewModel.CompleteSignIn: mọi vai trò, gồm Officer, vào Dashboard trước theo yêu cầu mới nhất. Luồng đổi mật khẩu bắt buộc cũng gọi CompleteSignIn nên cùng quy tắc.
- Double-click ô chữ trong bảng hồ sơ chọn toàn bộ nội dung ô. Card Việc cần xử lý thu vùng TextBox theo chữ; kiểm tra vị trí ký tự để chỉ hiện I-beam trên chữ, phần trắng hiện Hand và click mở chi tiết. Kéo chọn chữ vẫn được giữ khi con trỏ đi ra khoảng trắng.
- Danh sách hồ sơ và card Việc cần xử lý: nội dung dùng TextBox chỉ đọc với style SelectableDisplayText, cho bôi đen và copy từng phần bằng Ctrl+C/menu chuột phải. DataGridCopyBehavior nhường Ctrl+C khi đang chọn chữ; template cột giữ ClipboardContentBinding/SortMemberPath. Focus ô chữ vẫn chọn/tô sáng hàng. Build và 5 ca smoke pass; chưa kiểm tra kéo chuột trực tiếp.
- Danh sách hồ sơ chọn dòng bằng click: SelectionUnit FullRow/SelectionMode Single, tô nền xanh nhạt và chữ xanh giống bảng cán bộ. Checkbox chọn nhiều hồ sơ vẫn dùng IsSelected riêng của dữ liệu. Release solution build và 5 ca smoke pass.
- Header Tỷ lệ đúng hạn không xuống dòng, cột có MinWidth 145; glyph icon info dùng FontWeight Normal trong style dùng chung.
- Bảng danh sách cán bộ: thêm icon info cạnh header Tỷ lệ đúng hạn, KPI và Trạng thái. Tooltip giải thích mẫu số gồm cả hồ sơ chưa giải quyết có hạn, làm tròn phần trăm, mặc định 100% nếu không có hồ sơ theo dõi hạn; KPI bằng tỷ lệ này, trạng thái theo ngưỡng 90%/80%. Chỉ icon hiện tooltip, không đổi công thức. Verify: client build và 5 ca smoke pass.
- Thêm icon info cạnh tiêu đề card ở Tổng quan, Phân loại & xử lý và Theo dõi cán bộ; rê chuột hiển thị giải thích tiếng Việt.
- Nội dung bám cách đếm hiện tại, phân biệt trạng thái giữa các trang và số hồ sơ/số cán bộ theo vai trò. Chỉ bổ sung giao diện, không đổi truy vấn hay bộ lọc.
- Dùng chung style `MetricInfoButton` trong `App.xaml` và `Presentation/Converters/MetricDescriptionConverter.cs`.
- Verify: Debug client/server build output riêng và Release solution build pass; `tests/Scripts/run-smoke.ps1` pass 5 ca (1 unit + 4 integration). Chưa kiểm tra rê chuột trực tiếp trong phiên này.

## Điều chỉnh 2026-09-13 - Nhóm hồ sơ trong nút Xem

- Chi tiết danh sách chỉ hiển thị hồ sơ gốc và các lần gửi lại liên kết cùng gốc. Hồ sơ tiếp nhận mới độc lập có nhóm riêng, kể cả cùng người gửi.
- Popup nhập vẫn tra toàn bộ lịch sử để cán bộ chọn đúng hồ sơ gốc; không tự gộp hồ sơ mới.
- Verify mới: `run-all.ps1 -IncludeUI` pass 75 ca (40 unit/ViewModel + 34 integration + 1 UI smoke).

## Điều chỉnh 2026-09-13 - Popup đối chiếu

- Đã đổi đối chiếu từ Window sang popup overlay ngay trong trang nhập, không dùng ShowDialog.
- Tự chọn hồ sơ gốc đủ điều kiện; nút gửi lại có binding command và lý do rõ khi hồ sơ chưa giải quyết/khác vụ việc/chính là hồ sơ gửi lại. Hủy giữ form, lưu thành công đóng popup, lỗi giữ popup.
- `run-all.ps1 -IncludeUI` pass 74 ca: 40 unit/ViewModel + 33 integration + 1 UI smoke; harness render WPF kiểm tra binding, chọn hồ sơ và lưu bằng service giả. Chi tiết `AI/features/RECORD_RESUBMISSION.md`.

## Snapshot 2026-09-13 - Hồ sơ gửi lại

- Đã triển khai phương án người dùng duyệt: nhập mới đối chiếu lịch sử người gửi không giới hạn ngày; cán bộ chọn lưu như hồ sơ mới hoặc gửi lại hồ sơ đã giải quyết, bắt buộc ghi lý do.
- Hồ sơ gửi lại giữ mã/ngày nhận/nội dung/tài liệu riêng, trạng thái `Đã giải quyết — hồ sơ gửi lại`, liên kết hồ sơ gốc; không vào hàng chờ, không tăng giải quyết/KPI, không tạo lịch sử xác minh giả. Có lịch sử người gửi và liên kết xem từng hồ sơ trong chi tiết danh sách.
- Có chuẩn hóa tên/điện thoại và SenderId do server xác định; tên đơn lẻ không đủ nhận diện. Chưa có chức năng hợp nhất người gửi khi thay tên/số điện thoại.
- Schema thêm 3 cột metadata và 2 index, migration lặp lại an toàn. Client/server phải cập nhật cùng nhau; route mới `records/sender-history`, `records/save-resubmission`.
- Server bảo vệ liên kết khi sửa/xóa/mở lại; hồ sơ gốc còn tham chiếu không được xóa/đổi mã/đổi người gửi hoặc vụ việc/mở lại xử lý.
- Verify mới: Release solution build pass; `run-all.ps1 -IncludeUI` pass 71 ca (37 unit/ViewModel + 33 integration + 1 UI smoke); copied legacy DB migration và `PRAGMA quick_check = ok`; render dialog/detail bằng dữ liệu tổng hợp. UI smoke chỉ bảo vệ startup/login; chưa tự động click-through luồng mới có đăng nhập.
- Chi tiết: `AI/features/RECORD_RESUBMISSION.md`; test inventory: `tests/TEST_MATRIX.md`.

## Snapshot 2026-09-13 - Automated regression tests

- Da them 3 test projects vao solution, tach biet trong `tests/`: UnitTests, IntegrationTests, UITests. Framework: xUnit, Moq, Coverlet, FlaUI UIA3; test projects target .NET 8, app target .NET 5 khong doi.
- Ket qua da verify ngay 2026-09-12: Release solution build pass; 36 unit/ViewModel + 20 SQLite integration + 1 WPF UI smoke = 57 test cases pass. Lenh full da verify: `tests/Scripts/run-all.ps1 -IncludeUI`. Day la ket qua lich su, khong thay cho viec chay lai sau thay doi.
- Tests bao ve password/auth/role, commands/notifications, forced password-change validation, area selector, CRUD/reopen/Unicode/attachments metadata, search/pagination, rollback khi trung ma, trash/stale undo/permanent delete, backup/restore/corrupt backup.
- Production testability changes: internal `AppDataService(string databasePath)` khong tao LAN listener; `InternalsVisibleTo` cho IntegrationTests; path override `QUANLYHOSO_TEST_ROOT` chi khi env duoc set; 4 AutomationIds login; root csproj exclude `tests/**` khoi Compile. Khong doi UI visual hay business rules.
- Moi integration test tao DB rieng trong OS temp va cleanup; UI smoke launch child process voi profile rieng. Generated reports ignored trong git.
- Chua cover full processing transitions, export contents, LAN failures/concurrency, document generation, file locks/permission denial va authenticated UI workflows. Khong coi bo test hien tai la full feature coverage.
- Huong dan chay/duyet test: `AI/infra/TESTING.md`, `tests/README.md`; inventory: `tests/TEST_MATRIX.md`.

## Snapshot 2026-09-03

- Repo da tach `QuanLyHoSo.Shared`, `QuanLyHoSo.Core`, `QuanLyHoSo.Server`.
- `QuanLyHoSo.Server` la console server giu SQLite DB/log/API. Chay bang:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --url http://0.0.0.0:5055
```

- WPF `QuanLyHoSo` mac dinh la `Client`. Neu `DataAccessMode` thieu/lạ thi normalize ve `Client`; chi `AdminHost` ro rang moi chay local DB.
- Trang Settings WPF khong con popup cai dat DB/log/url. Cac thong so do thuoc server.
- Settings van co catalog, user management, system logs, update software va backup.
- Backup trong Settings la server-side: admin bam Sao luu ngay -> client goi `settings/backup/create` -> server tao file trong `%LocalAppData%\QuanLyHoSo\Backup` tren may server.
- Admin trong WPF client van co Nhap du lieu va luu/sua/xoa ho so qua LAN API (`records/similar`, `records/save`, `records/delete`).
- Nut sua ho so o danh sach duoc mo theo `AuthContext.CanEditRecord(record.ProcessorName)`, khong khoa theo client mode.
- Officer/can bo khong thay muc Nhap du lieu, khong them/xoa ho so; chi xem/chinh sua/phan loai theo quyen va khong duoc lui workflow ve buoc truoc phan cong.
- Attachment LAN hien moi luu metadata/path; chua co upload/copy file vat ly tu client len server.

## Snapshot 2026-09-09

- `QuanLyHoSo.Server` da co giao dien WPF quan tri co ban thay cho cua so console.
- Giao dien server compact, chi hien mot URL ket noi client dung duoc, ten may, uptime va so client dang ket noi; khong hien URL lang nghe/duong dan DB/noi dung log ky thuat.
- Cham xanh trang thai server pulse cham khi listener dang chay.
- Client gui heartbeat 30 giay kem header `X-QuanLyHoSo-Client`; server dem ten may duy nhat co request/heartbeat trong 90 giay gan nhat.
- Admin tai may server co the start/stop LAN server, reset built-in `admin`, mo thu muc data/log va copy URL cho client. Sao luu du lieu thuc hien trong Settings cua app WPF sau khi Admin dang nhap.
- Reset Admin tren server tao mat khau tam 8 ky tu cho username `admin`, co nut copy va bat buoc doi mat khau khi dang nhap.
- User moi hoac user duoc Admin cap lai mat khau deu phai doi mat khau tren man hinh rieng truoc khi vao shell/sidebar.
- Dong/thu nho cua so se an xuong system tray; chi nut `Thoat may chu` hoac menu tray moi dung server.
- Tham so cu van dung: `--url`, `--database`, `--log-folder`, `--sample-data`.
- Day van la tray application theo user session, chua phai Windows Service tu chay truoc khi dang nhap Windows.
- File log ky thuat `quanlyhoso-yyyyMMdd.log` tu dong chi giu 30 ngay; cleanup moi ngay mot lan va khong xoa file sai mau/khac ten.
