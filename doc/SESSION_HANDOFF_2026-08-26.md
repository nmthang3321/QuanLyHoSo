# Session handoff - QuanLyHoSo

Cap nhat: 2026-09-01

Muc tieu file nay: giup AI tiep theo vao viec nhanh, khong doc ca repo. Doc file nay truoc, sau do chay `git status --short --branch`.

## Trang thai git

- Branch: `main`
- Remote: `origin https://github.com/nmthang3321/QuanLyHoSo.git`
- Commit moi nhat da push: `ef6a947 Add LAN server client prototype`
- Working tree hien dang co thay doi chua commit cho tinh nang dia ban hierarchical menu:
  - `Infrastructure\Data\AppDataService.cs`
  - `Models\AreaSelectionModels.cs` (file moi)
  - `ViewModels\RecordInputViewModel.cs`
  - `ViewModels\RecordListViewModel.cs`
  - `ViewModels\RecordProcessingViewModel.cs`
  - `Views\Records\RecordInputView.xaml`
  - `Views\Records\RecordInputView.xaml.cs`
  - `Views\Records\RecordListView.xaml`
  - `Views\Records\RecordListView.xaml.cs`
  - `doc\SESSION_HANDOFF_2026-08-26.md`

## Verify

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build
```

Warning `NETSDK1138` ve `.NET 5.0-windows` het support la warning cu. Build OK neu 0 error.

Lan verify gan nhat cho thay doi dia ban:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build-area-root-overlay-final
```

Ket qua: build OK, 0 error, chi con warning `NETSDK1138`.

## Tong quan

- App: WPF desktop, C#, MVVM tu viet, `.NET 5.0-windows`.
- DB: SQLite.
- Local DB mac dinh: `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso.db`.
- Log: `%LocalAppData%\QuanLyHoSo\Logs\quanlyhoso-yyyyMMdd.log`.
- Settings path: `%LocalAppData%\QuanLyHoSo\Settings\path-settings.json`.
- Data service chinh: `Infrastructure\Data\AppDataService.cs`.
- Shell/navigation: `ViewModels\ShellViewModel.cs`, `MainWindow.xaml`.
- Logger: `Infrastructure\Logging\AppLogger.cs`.

## Role va phan quyen

File chinh:
- `Models\AuthModels.cs`
- `Infrastructure\Security\AuthContext.cs`
- `ViewModels\LoginViewModel.cs`
- `Views\Auth\LoginView.xaml`

Role:
- `Admin`: toan quyen.
- `Leader`: chi xem, khong sua.
- `Officer`: chi toan quyen tren ho so co `Records.ProcessorName == AuthContext.CurrentDisplayName`.

Dang co login, logout, quan ly user trong Settings. User mac dinh seed: `admin/admin123`.

## Kien truc LAN prototype

Da lam ban don gian: **1 may server/admin giu DB, client khong mo SQLite truc tiep**.

File chinh:
- `Infrastructure\Configuration\AppPathSettings.cs`
- `Infrastructure\Network\LanApiModels.cs`
- `Infrastructure\Network\LanDataClient.cs`
- `Infrastructure\Network\LanDataServer.cs`
- `Infrastructure\Network\LanServerUnavailableException.cs`
- `Infrastructure\Data\AppDataService.cs`
- `scripts\test-lan-local.ps1`

Mode:
- `AdminHost`: mo SQLite local, seed/schema, dong thoi bat API noi bo LAN tai `AdminServerUrl`.
- `Client`: khong tao/mo SQLite, chi goi HTTP API toi may admin.

Config mau server:

```json
{
  "DatabasePath": "C:\\QuanLyHoSo\\Data\\quanlyhoso.db",
  "LogFolder": "C:\\QuanLyHoSo\\Logs",
  "DataAccessMode": "AdminHost",
  "AdminMachineName": "MAY-ADMIN-01",
  "AdminServerUrl": "http://192.168.1.10:5055"
}
```

Config mau client:

```json
{
  "DatabasePath": "",
  "LogFolder": "C:\\QuanLyHoSo\\Logs",
  "DataAccessMode": "Client",
  "AdminMachineName": "MAY-ADMIN-01",
  "AdminServerUrl": "http://192.168.1.10:5055"
}
```

Neu server tat/mat mang, client hien popup qua `LanServerUnavailableException`: can bat app/server admin, cung LAN, firewall mo port 5055.

Test local:

```powershell
.\scripts\test-lan-local.ps1
```

Script se build, backup settings, mo 1 app `[SERVER]`, doi config sang `Client`, mo 1 app `[CLIENT]`, va restore settings khi dong app. Title cua cua so phan biet bang `MainWindow.xaml.cs`: `[SERVER]` hoac `[CLIENT]`.

Hien tai LAN prototype da noi cac luong chinh:
- login
- dashboard/read stats
- danh sach ho so/filter/export preview
- xem chi tiet ho so
- queue xu ly
- cap nhat xu ly
- xoa ho so qua server

Chua lam day du:
- nhap moi/sua form ho so tu client qua API, dac biet attachment/upload file.
- service Windows chay nen rieng; hien server API dang nhung trong app admin.
- dong bo nhieu admin host. Khuyen nghi hien tai: 1 admin host giu DB, admin phu chay `Client` nhung dang nhap role `Admin`.

## Cac trang/file can mo

### Dashboard

- `ViewModels\DashboardViewModel.cs`
- `Views\Dashboard\DashboardView.xaml`
- `Views\Dashboard\DashboardView.xaml.cs`
- `Models\DashboardModels.cs`
- Service methods: `GetDashboardMetrics`, `GetStatusStats`, `GetTopAreas`, `GetReceivedTrendStats`, `GetRecentRecords`, `CountRecords`.

Ghi chu: da fix not responding do `CalculateNiceAxisStep` tra 0; reload dashboard dang async/background.

### Nhap du lieu

- `ViewModels\RecordInputViewModel.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- Service methods: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, `DeleteRecord`.

Client mode hien chua cho vao trang nhap/sua ho so de tranh mo SQLite; can noi API rieng neu user yeu cau.

### Dia ban hierarchical menu

Yeu cau moi nhat cua user: input dia ban can co search/filter bang go text va van giu hierarchy theo group. Khong dung `Popup`/`ContextMenu` cho o search vi bo go tieng Viet/IME co the hien edit box o goc trai man hinh.

File chinh:
- `Models\AreaSelectionModels.cs`
- `Infrastructure\Data\AppDataService.cs`
- `ViewModels\RecordInputViewModel.cs`
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`

Nhom dia ban theo thu tu tu cap nho den cap lon:
- `Cap xa`: danh sach 102 xa/phuong/dac khu tu bang `Areas`.
- `Cap tinh`: `Tinh uy An Giang`, `Uy ban nhan dan tinh`, `Ban Noi chinh Tinh uy`, `Thanh tra tinh`.
- `Cap bo`: `C01`, `C02`, `C03`, `C04`, `X05`, `X06`.
- `Cong an tinh`: `PC02`, `PC03`, `PC04`, `PX05`, `PX06`, `Don vi khac trong tinh`.
- `Don vi trong nganh ngoai tinh`: 1 option cung ten.

Behavior hien tai:
- Trang nhap lieu: button mo panel inline tren root overlay `AreaOverlayCanvas`, khong phai popup, khong lam gian layout doc va khong bi card/section khac cat. Code-behind tinh vi tri theo `AreaDropDownButton` bang `TransformToVisual(AreaOverlayCanvas)`; khong dung `TransformToAncestor` vi canvas la sibling, se crash. Panel co textbox search, group header bung/thu bang click; khi dang search thi group tu bung va chi hien item khop. Chon item set `AreaName = option.FilterValue`.
- Trang danh sach ho so: button mo `ContextMenu`; co the chon `Tat ca`, group, hoac item con. Group filter xu ly bang SQL theo `AreaType`/tap `AreaName`.
- Da bo tooltip khoi menu item.
- Trang nhap lieu dang dung `FilteredAreas`, `AreaSearchText`, `AreaSelectionOptions.Filter/Flatten` de search. Trang danh sach ho so hien van dung menu chon, chua co search.
- `AppDataService.Initialize()` goi `EnsureStandardOrganizationAreas(connection)` de dam bao cac don vi cap tinh/bo/cong an tinh/ngoai tinh co trong bang `Areas`.
- `GetAreaNames()` format xa/phuong/dac khu thanh `"AreaType Name"`; cac don vi to chuc tra ve `Name` de khong hien tien to thua.
- Cac filter record/export/processing queue dung `AddOptionalAreaFilter()`; manual save/update `AreaName = $areaName` khong doi.

### Danh sach ho so

- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordListRowViewModel.cs`
- `Views\Records\RecordListView.xaml`
- Service methods: `GetFilteredRecords`, `CountFilteredRecords`, `GetExportPreview`, `DeleteRecord`, `GetRecordForm`.

Export Excel nam trong trang nay. Page `Export` rieng da bo.

### Phan loai & xu ly

- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordProcessingView.xaml`
- Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, `CountProcessingQueueRecords`, `GetProcessingRecordDetail`, `UpdateProcessingRecord`.

Officer chi sua ho so dung ten minh; Leader chi xem.

### Cai dat

- `ViewModels\SettingsViewModel.cs`
- `Views\Settings\SettingsView.xaml`
- `Models\SettingsModels.cs`
- Service methods: user/catalog/system log/backup/restore.

Dang co:
- quan ly danh muc
- popup quan ly user
- cai dat chung: DB path, log folder, `DataAccessMode`, `AdminMachineName`, `AdminServerUrl`
- system logs
- backup/restore
- update software

## Database/schema chinh

Bang:
- `Users`
- `Areas`
- `CatalogItems`
- `Records`
- `RecordAttachments`
- `ProcessHistories`
- `SystemLogs`

Catalog `ProcessorName` duoc sync tu `Records.ProcessorName`.

`TryAddColumn` da doi sang check `PRAGMA table_info` truoc khi `ALTER TABLE`, de khong con warning lap lai `duplicate column name: FilePath`.

## Quy tac sua code

- Dung `rg` de tim method/binding.
- Dung `apply_patch` khi edit.
- Khong revert thay doi user.
- Khong doc ca `AppDataService.cs`; tim method bang:

```powershell
rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5
```

- Neu task lien quan UI: mo ViewModel + XAML cua trang do truoc.
- Neu task lien quan LAN/client/server: mo `AppPathSettings`, `LanDataClient`, `LanDataServer`, `AppDataService`.
- Neu task build bi khoa exe, build ra output rieng:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build-something
```

`.gitignore` da ignore `.verify-build/`, `.verify-build-*/`, `.lan-test-build/`.
