# Database mẫu

`quanlyhoso-demo.db` là database SQLite dùng để chạy thử ứng dụng. Dữ liệu gồm 105 hồ sơ được chia đều cho 7 cán bộ, mỗi cán bộ 15 hồ sơ, với nhiều trạng thái và thời hạn xử lý khác nhau để quan sát trang Theo dõi cán bộ.

## Chạy server với dữ liệu mẫu

Từ thư mục gốc của repository, chạy:

```powershell
dotnet run --project .\QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --sample-data --url http://0.0.0.0:5055
```

Mỗi lần server được khởi động với `--sample-data`, file mẫu trong repository sẽ được sao chép mới tới `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso-sample.db`. Vì vậy dữ liệu phát sinh khi thử nghiệm không làm thay đổi file mẫu được lưu trong Git.

## Tài khoản đăng nhập

| Vai trò | Tên đăng nhập | Mật khẩu | Cán bộ |
| --- | --- | --- | --- |
| Admin | `admin` | `admin123` | Quản trị hệ thống |
| Lãnh đạo | `leader` | `leader123` | Lê Thành Vinh |
| Cán bộ | `officer1` | `officer123` | Lê Thị D |
| Cán bộ | `officer2` | `officer123` | Lê Võ Mỹ Ý |
| Cán bộ | `officer3` | `officer123` | Nguyễn Minh Thắng |
| Cán bộ | `officer4` | `officer123` | Nguyễn Thị H |
| Cán bộ | `officer5` | `officer123` | Phạm Văn K |
| Cán bộ | `officer6` | `officer123` | Trần Văn B |
| Cán bộ | `officer7` | `officer123` | Trần Văn C |

Database này chỉ dành cho phát triển và trình diễn, không dùng làm dữ liệu thật.
