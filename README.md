# Phần mềm Quản lý hồ sơ

Phần mềm hỗ trợ tiếp nhận, phân loại, theo dõi xử lý và quản lý hồ sơ tại An Giang.

> **Trạng thái dự án:** Phần mềm đang trong giai đoạn thử nghiệm và hoàn thiện yêu cầu, chưa phải phiên bản phát hành chính thức.

## Thành phần cài đặt

- **QuanLyHoSo Server**: cài trên máy chủ lưu trữ dữ liệu.
- **QuanLyHoSo Client**: cài trên các máy trạm và kết nối tới máy chủ trong mạng nội bộ.

Server và Client được quản lý phiên bản độc lập. Chức năng **Cập nhật phần mềm** trong ứng dụng chỉ cập nhật Client; không cần nâng cấp Server chỉ để khớp số phiên bản với Client.

Dữ liệu nghiệp vụ, file đính kèm và biểu mẫu được lưu tập trung trên Server. Chức năng sao lưu mặc định tạo gói `.qlhbackup` gồm cơ sở dữ liệu cùng toàn bộ file do ứng dụng quản lý; file `.db` cũ vẫn được hỗ trợ để tương thích nhưng chỉ chứa dữ liệu cơ sở dữ liệu.
