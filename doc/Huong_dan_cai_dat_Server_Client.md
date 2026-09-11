# Hướng dẫn cài đặt QuanLyHoSo 1.0.2

## 1. Cài máy Server

1. Chép `QuanLyHoSo-Server-Setup-1.0.2-win-x64.exe` vào máy được chọn làm Server.
2. Nhấp đúp bộ cài và đồng ý yêu cầu quyền Administrator của Windows.
3. Nên chọn **Tự khởi động Server khi đăng nhập Windows**.
4. Hoàn tất cài đặt và mở **QuanLyHoSo Server**.
5. Trên bảng điều khiển Server, ghi lại **Địa chỉ Client**, ví dụ `http://SERVER-PC:5055` hoặc `http://192.168.1.10:5055`.
6. Giữ Server chạy khi các máy Client sử dụng phần mềm. Có thể thu nhỏ cửa sổ xuống khay hệ thống.

Bộ cài Server tự đăng ký URL và mở cổng TCP `5055` cho các thiết bị trong cùng mạng LAN. Dữ liệu chính nằm tại:

```text
C:\ProgramData\QuanLyHoSo\Data\quanlyhoso.db
```

## 2. Cài từng máy Client

1. Đảm bảo máy Client cùng mạng LAN với máy Server.
2. Chép và chạy `QuanLyHoSo-Client-Setup-1.0.2-win-x64.exe`.
3. Tại bước **Kết nối máy chủ**, nhập đúng địa chỉ hiển thị trên máy Server.
4. Bộ cài sẽ thử kết nối. Nếu chưa kết nối được, kiểm tra Server đang chạy và địa chỉ đã nhập đúng.
5. Hoàn tất cài đặt rồi mở **QuanLyHoSo Client**.

Ví dụ URL hợp lệ:

```text
http://SERVER-PC:5055
http://192.168.1.10:5055
```

Không nhập `http://0.0.0.0:5055` trên Client. Địa chỉ `0.0.0.0` chỉ dành cho Server lắng nghe kết nối.

## 3. Cài Client tự động

Có thể truyền URL bằng tham số khi cài:

```powershell
.\QuanLyHoSo-Client-Setup-1.0.2-win-x64.exe /SERVERURL="http://SERVER-PC:5055"
```

## 4. Lưu ý

- Hai bộ cài dành cho Windows 64-bit.
- Client không cần quyền Administrator.
- Server chỉ cần quyền Administrator trong quá trình cài hoặc gỡ để cấu hình mạng.
- Hai ứng dụng đã kèm .NET Runtime, máy đích không cần cài .NET riêng.
- Bộ cài chưa có chữ ký số của nhà phát hành nên Windows SmartScreen có thể hiện cảnh báo **Unknown publisher**. Chỉ tiếp tục nếu file nhận từ nguồn bàn giao tin cậy và mã SHA-256 khớp file `SHA256.txt`.
