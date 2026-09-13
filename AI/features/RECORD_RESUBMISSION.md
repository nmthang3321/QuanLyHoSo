# Hồ sơ gửi lại / lịch sử người gửi

Cập nhật: 2026-09-13. Thay đổi nghiệp vụ được người dùng duyệt.

- Khi lưu hồ sơ mới, `GetSenderRecords(draft)` đối chiếu toàn bộ lịch sử người gửi, không giới hạn ±30 ngày. Popup overlay `SenderHistoryDialog` (UserControl nằm trong trang nhập, không mở Window) hiển thị nội dung và lịch sử/kết quả xử lý để cán bộ xác nhận.
- Chọn lưu như hồ sơ mới: trạng thái `Mới tiếp nhận`, vào luồng xử lý hiện có. Chọn hồ sơ đã giải quyết cùng người gửi/địa bàn/loại vụ việc và ghi lý do: tạo mã mới với trạng thái `Đã giải quyết — hồ sơ gửi lại`, liên kết `OriginalRecordCode`; không tạo `ProcessHistories` hoặc tài liệu kết quả giả.
- Không tự kết luận trùng nội dung. Cán bộ phải đối chiếu cùng vụ việc, không có tình tiết mới. Hồ sơ chưa giải quyết, hồ sơ gửi lại hoặc hồ sơ trong thùng rác không được chọn làm hồ sơ gốc.
- Chuẩn hóa tên: trim, gộp khoảng trắng, bỏ dấu, không phân biệt hoa/thường. Điện thoại: chỉ giữ chữ số, đổi +84/0084 thành 0. Nhận diện bằng tên + điện thoại; khi cả hai bên không có điện thoại thì dùng tên + địa chỉ. Tên đơn lẻ không đủ.
- `SenderId` được tạo/tra cứu tại server, không tin ID tùy ý từ client; các hồ sơ cũ cùng thông tin được gán ID trong giao dịch khi lưu. Đổi thông tin nhận diện trên hồ sơ thường sẽ tra lại ID, tránh giữ lịch sử của người khác. Chưa có màn hình hợp nhất người gửi khi thay tên/số điện thoại.
- Chi tiết danh sách chỉ có hồ sơ gốc và các lần gửi lại cùng `OriginalRecordCode`; hồ sơ tiếp nhận mới độc lập không nằm trong nhóm, kể cả cùng người gửi; bấm mã để xem từng hồ sơ, nội dung/tài liệu và kết quả từ hồ sơ gốc. Officer chỉ thấy hồ sơ trong phạm vi quyền hiện có.
- Hồ sơ gửi lại tính vào tổng tiếp nhận, phân bố trạng thái, danh sách và xuất dữ liệu. Không tăng số đã giải quyết, tiến độ xử lý, KPI cán bộ, hồ sơ quá hạn hoặc hàng chờ. Nút phân loại bị ẩn; server chặn cập nhật xử lý kể cả admin.
- Sửa form giữ liên kết/lý do ban đầu, kiểm tra người gửi/địa bàn/loại vụ việc với hồ sơ gốc. Hồ sơ gốc còn hồ sơ gửi lại tham chiếu không được xóa, đổi mã, đổi thông tin nhận diện/vụ việc hoặc mở lại xử lý; bảo vệ kết quả tham chiếu.
- Schema bổ sung không phá dữ liệu: `Records.SenderId`, `OriginalRecordCode`, `ResubmissionReason` (`TEXT NOT NULL DEFAULT ''`) và hai index. Khởi tạo lặp lại an toàn; chạy lại sau migration legacy PriorityLevel.
- LAN: `records/sender-history` nhận `RecordFormDraft`; `records/save-resubmission` nhận `SaveRecordFormRequest`. Client dùng route riêng khi có liên kết để không vô tình lưu hồ sơ xử lý mới trên server cũ. Cần cập nhật cả client và server.

Files: `Models/RecordModels.cs`, `Application/Abstractions/IApplicationDataService.cs`, `Infrastructure/Data/AppDataService.cs`, `Infrastructure/Network/LanDataServer.cs`, `ViewModels/RecordInputViewModel.cs`, `ViewModels/RecordListViewModel.cs`, `ViewModels/RecordListRowViewModel.cs`, `ViewModels/RecordProcessingViewModel.cs`, `Views/Records/SenderHistoryDialog.xaml[.cs]`, `Views/Records/RecordInputView.xaml`, `Views/Records/RecordListView.xaml`, `Presentation/Converters/StatusToBrushConverter.cs`.

Tests: `RecordResubmissionTests.cs`, `RecordResubmissionViewModelTests.cs`. Migration kiểm tra trên bản sao DB kiểm thử; không sửa DB thật. Render preview dùng dữ liệu tổng hợp, không thay cho kiểm thử click-through có đăng nhập.

## Điều chỉnh popup 2026-09-13

- Đối chiếu chuyển từ Window/ShowDialog sang overlay trong `RecordInputView`, command/state ở `RecordInputViewModel`; khóa form phía sau khi popup mở, Escape/nút ×/Hủy lưu đóng và giữ form.
- Tự chọn hồ sơ gốc đủ điều kiện thay vì dòng mới nhất (có thể là hồ sơ gửi lại/chưa giải quyết). Chọn cả dòng; nút gửi lại theo command của ViewModel. Hiển thị lý do không đủ điều kiện hoặc thiếu lý do xác nhận ngay trong popup.
- Lưu tiếp từ draft đang chờ, giữ popup khi server báo lỗi. Lưu thành công đóng popup và hiển thị trạng thái gửi lại trên form; không mở thêm hộp thông báo thành công.
- Verify: 74 ca pass (40 Unit/ViewModel + 33 integration + 1 UI smoke). Ba test mới bảo vệ tự chọn hồ sơ gốc/lý do/lưu gửi lại, hủy giữ form, và lưu như hồ sơ mới. Harness WPF với service giả đã kiểm tra binding/nút bật-tắt/lưu, render popup trong trang.

## Tách nhóm hồ sơ trong chi tiết

Theo yêu cầu tiếp theo của người dùng, `GetRecordForm` chỉ trả `SenderHistory` thuộc nhóm của hồ sơ đang xem: mã hồ sơ gốc bằng mã đang xem nếu là hồ sơ độc lập, hoặc `OriginalRecordCode` nếu là lần gửi lại. Gồm hồ sơ gốc và các hồ sơ liên kết gửi lại gốc đó. Nhãn UI: “Hồ sơ gốc và các lần gửi lại”. `GetSenderRecords` trong popup nhập vẫn đối chiếu toàn bộ lịch sử để chọn đúng hồ sơ gốc. Test `RecordDetail_ShouldShowOnlyItsOriginalAndResubmissionsNotIndependentIntakes` bảo vệ hai nhóm độc lập cùng người gửi và trường hợp hồ sơ mới chưa có lần gửi lại.
