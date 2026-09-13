# Infra - Automated testing

Cap nhat: 2026-09-13

## Muc tieu va routing

Bao ve behavior hien co, uu tien data integrity, authorization va regression. Khong doi UI, validation, workflow hoac database semantics chi de lam test de hon.

- Huong dan executable: `tests/README.md`.
- Risk/test inventory va gaps: `tests/TEST_MATRIX.md`.
- Scripts: `tests/Scripts/`.
- Generated reports: `tests/Reports/` (gitignored, chi giu `.gitkeep`).

## Architecture

- `tests/QuanLyHoSo.UnitTests`: xUnit + Moq; password hashing, AuthContext role/record permissions, RelayCommand, ViewModelBase notifications/disposal, area selection/filter, RequiredPasswordChangeViewModel validation/success/failure/cancel.
- `tests/QuanLyHoSo.IntegrationTests`: xUnit + SQLite that qua Core; init/seeds/auth/users, create/read/update/reopen/Unicode/attachment metadata, search/pagination, permission denial, failed-update rollback, trash/restore/permanent delete, backup/restore/corrupt input.
- `tests/QuanLyHoSo.UITests`: xUnit + FlaUI UIA3; launch executable WPF that, verify login title/controls qua AutomationId, dong app.
- Test projects target `net8.0` / `net8.0-windows`; production projects van target .NET 5.
- Coverlet collector tao Cobertura coverage khi chay regression. Coverage tung test run la rieng, khong cong ty le thanh tong coverage.

## Lenh chay

Chay tai repository root bang PowerShell:

```powershell
# Sau thay doi code
.\tests\Scripts\run-smoke.ps1

# Tung layer
.\tests\Scripts\run-unit.ps1
.\tests\Scripts\run-integration.ps1
.\tests\Scripts\run-ui.ps1

# Noninteractive regression + coverage
.\tests\Scripts\run-regression.ps1

# Full noninteractive; them UI khi co desktop interactive
.\tests\Scripts\run-all.ps1
.\tests\Scripts\run-all.ps1 -IncludeUI
```

Neu execution policy chan script:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Scripts\run-all.ps1
```

- Can Windows, PowerShell 5.1+, .NET 8 SDK va .NET 5 Desktop Runtime.
- UI can session interactive, khong khoa man hinh; khong bat UI trong headless CI.
- Scripts build solution truoc, fail neu build/test loi, tao TRX. `run-all` build mot lan; khong tu y dung `-SkipBuild` khi binary co the cu.
- Categories la xUnit `Trait("Category", ...)`: Smoke, Unit, Integration, Database, Security, Critical, Regression, Backup, Search, Pagination, Negative, UI. Mot test co the co nhieu category; category khong dong nghia voi full feature coverage.

## Isolation va production seams

- `TestDatabase` tao unique directory duoi OS temp, DB/log rieng, `AppPathSettings.UseServerMode` chi cap nhat state trong memory; cleanup ca khi initialize fail.
- IntegrationTests disable parallelization vi AuthContext va AppPathSettings la process-wide mutable state.
- Internal `AppDataService(string databasePath)` khong tao LAN listener, visible chi cho assembly `QuanLyHoSo.IntegrationTests`. Singleton constructor runtime khong doi.
- `QUANLYHOSO_TEST_ROOT` override Settings/default DB/default Logs chi khi env duoc set. UI test set tren child process de khong doc configured production database; khong set env nay global tren may nguoi dung.
- Login co AutomationIds `Login.UserName`, `Login.Password`, `Login.TogglePassword`, `Login.SignIn`; khong anh huong visual layout.
- Root `QuanLyHoSo.csproj` exclude `tests/**` khoi Compile de SDK glob khong keo test source vao app.
- Synthetic Vietnamese/Unicode data; seeded admin credentials chi duoc kiem tra tren DB temp moi tao. Khong dung real DB/accounts/files.

## Baseline da verify

Ngay 2026-09-12, Release build pass va `run-all.ps1 -IncludeUI` pass 57 test cases: 36 Unit/ViewModel, 20 Integration, 1 UI. Khong con app process hay test temp child directories sau execution. .NET 5 EOL warning `NETSDK1138` van con; khong nang framework production trong task testing.

Day la baseline lich su. Phai chay lai suite sau code change; khong bao cao baseline nay nhu ket qua execution moi.

## Gaps / next priorities

- P0: full processing-state transitions, forbidden/repeated transition va restart giua workflow.
- P1: export file contents (columns/rows/Unicode/filters), LAN protocol/auth/disconnect, concurrency/double save/delete/shutdown, file lock/permission failure, authenticated UI login/navigation/create/edit/status/logout.
- P2: generated Word document content/template/failure verification.
- Pagination hien moi co consecutive pages size 1; authorization data-layer tests moi la subset, khong phai moi endpoint. UI smoke chi verify startup/login surface/close, chua verify login thanh cong.

## Khi them/sua test

- Dat test code trong `tests/`, khong tron vao production folders.
- Test theo behavior that, assertion gia tri cu the; normal + boundary + negative paths.
- DB tests dung `TestDatabase` moi cho moi test; khong shared mutable dataset hay rely execution order.
- Bug confirmed: reproduce -> test fail -> fix co scope ro rang -> test pass -> giu regression test. Bao cao existing defects, khong silently doi business rules.
- UI selectors dung AutomationId/name/control type; condition-based timeout, khong coordinates/arbitrary sleeps.
- Update `tests/TEST_MATRIX.md` khi coverage thay doi; update file nay neu architecture/scripts/seams thay doi.
