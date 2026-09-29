# Phần mềm Quản lý hồ sơ

Phần mềm hỗ trợ tiếp nhận, phân loại, theo dõi xử lý và quản lý hồ sơ tại An Giang.

## Tài liệu dành cho người dùng

- [Hướng dẫn sử dụng](doc/Huong_dan_su_dung_QuanLyHoSo.md)
- [Hướng dẫn cài đặt Server và Client](doc/Huong_dan_cai_dat_Server_Client.md)
- [Tài liệu thiết kế tổng thể hệ thống (High Level Design)](doc/Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md)
- [Bộ tài liệu khách hàng bản PDF](doc/QuanLyHoSo_TaiLieu_KhachHang_1.0.0.pdf)

Các biểu mẫu Word được phần mềm sử dụng nằm trong `doc/templates/`.

## Thành phần cài đặt

- **QuanLyHoSo Server**: cài trên máy chủ lưu trữ dữ liệu.
- **QuanLyHoSo Client**: cài trên các máy trạm và kết nối tới máy chủ trong mạng nội bộ.

Server và Client được quản lý phiên bản độc lập. Chức năng **Cập nhật phần mềm** trong ứng dụng chỉ cập nhật Client; không cần nâng cấp Server chỉ để khớp số phiên bản với Client.

Dữ liệu nghiệp vụ, file đính kèm và biểu mẫu được lưu tập trung trên Server. Chức năng sao lưu mặc định tạo gói `.qlhbackup` gồm cơ sở dữ liệu cùng toàn bộ file do ứng dụng quản lý; file `.db` cũ vẫn được hỗ trợ để tương thích nhưng chỉ chứa dữ liệu cơ sở dữ liệu.

Thông tin chi tiết về cài đặt, đăng nhập và sử dụng các chức năng được trình bày trong các tài liệu hướng dẫn phía trên.
