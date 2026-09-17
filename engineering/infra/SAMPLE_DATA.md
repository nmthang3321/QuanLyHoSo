# Sample database

`quanlyhoso-demo.db` is a SQLite database for application demonstrations and development. It contains 105 records distributed evenly across seven officers, with 15 records per officer and varied processing statuses and deadlines for exercising the Staff Tracking page.

Normal production initialization does not create demo records. A fresh customer database starts with the built-in administrator, standard areas, and default catalogs, but with zero records. Demo records are enabled only by the explicit `--sample-data` server option.

## Run the server with sample data

From the repository root, run:

```powershell
dotnet run --project QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -- --sample-data
```

Each server start with `--sample-data` copies a fresh version of the repository sample to `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso-sample.db`. Changes made during testing therefore do not alter the sample database committed to Git.

## Sign-in accounts

| Role | Username | Password | Staff member |
|---|---|---|---|
| Admin | `admin` | `admin123` | Quản trị hệ thống |
| Leader | `leader` | `leader123` | Lê Thành Vinh |
| Officer | `officer1` | `officer123` | Lê Thị D |
| Officer | `officer2` | `officer123` | Lê Võ Mỹ Ý |
| Officer | `officer3` | `officer123` | Nguyễn Minh Thắng |
| Officer | `officer4` | `officer123` | Nguyễn Thị H |
| Officer | `officer5` | `officer123` | Phạm Văn K |
| Officer | `officer6` | `officer123` | Trần Văn B |
| Officer | `officer7` | `officer123` | Trần Văn C |

This database is for development and demonstration only. Do not use it as production data.
