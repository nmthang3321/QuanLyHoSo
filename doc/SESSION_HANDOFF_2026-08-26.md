# Session handoff - QuanLyHoSo

Cap nhat gan nhat: 2026-08-31

File nay la ban do ngan gon de AI tiep theo tiep tuc nhanh, tranh doc het repo va analyse nhung phan khong lien quan.

## Cach AI nen doc file nay

1. Doc file nay truoc khi mo code.
2. Chay `git status --short` de biet working tree dang dirty o dau.
3. Xac dinh user dang noi toi trang nao/chuc nang nao.
4. Chi mo cac file trong muc "Kien truc theo trang" tuong ung. Neu can tim them, dung `rg -n "keyword" <folder/file> -S`.
5. Khong doc toan bo `AppDataService.cs` neu task khong lien quan database. File nay dai; neu can thi mo theo ten method bang `rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5`.
6. Khi sua XAML/ViewModel, uu tien doc 80-180 dong quanh khu vuc can sua, khong dump ca file.
7. Sau khi sua code, build bang:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build
```

Warning `NETSDK1138` ve `net5.0-windows` het support la warning cu, hien build van OK neu khong co error khac.

## Tong quan du an

- Project: `QuanLyHoSo`
- Loai app: WPF desktop app, C#, MVVM tu viet, `.NET 5.0-windows`
- Workspace: `D:\PROJECT\QuanLyHoSo`
- Database local: `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso.db`
- Log file ky thuat: `%LocalAppData%\QuanLyHoSo\Logs`
- Service du lieu chinh: `Infrastructure\Data\AppDataService.cs`
- Logger file: `Infrastructure\Logging\AppLogger.cs`

## Quy tac lam viec trong repo

- Working tree co nhieu thay doi chua commit; khong revert thay doi khong lien quan.
- Khong khoi phuc lai trang `Xuat du lieu`; page nay da bi bo vi da tich hop filter/export vao `Danh sach ho so`.
- File `QuanLyHoSo.csproj.user`, `bin/`, `obj/`, `.verify-build/`, local data/log khong nen commit.
- Noi dung UI dung tieng Viet co dau. Neu PowerShell hien mojibake, dung tin vao source UTF-8 trong file/app hon la output terminal.
- ViewModel giu state/command/collection. Code-behind chi xu ly behavior UI nhu drag/drop, mouse wheel, animation.

## Kien truc chung

- `App.xaml`: resource mau/style chung, DataTemplate ViewModel -> View, converters, DataGrid copy behavior.
- `MainWindow.xaml`, `MainWindow.xaml.cs`: khung shell, sidebar, content.
- `ViewModels\ShellViewModel.cs`: initialize DB, tao page ViewModel, navigation/sidebar highlight.
- `Infrastructure\Data\AppDataService.cs`: schema, seed data, query SQLite. Chi mo method lien quan task.
- `Models\*.cs`: DTO/bindable model.
- `Presentation\*.cs`: converter/helper/behavior UI dung chung.

## Kien truc theo trang

### 1. Tong quan

Dung khi user noi `Tong quan`, `dashboard`, `bieu do`, `filter ngay`, `donut`, `top dia ban`, `trend`.

File can doc:
- `ViewModels\DashboardViewModel.cs`
- `Views\Dashboard\DashboardView.xaml`
- `Views\Dashboard\DashboardView.xaml.cs`
- `Models\DashboardModels.cs`
- Neu can query: cac method dashboard trong `Infrastructure\Data\AppDataService.cs`

Da lam:
- Dashboard mac dinh loc `Nam nay`.
- Co metric cards, donut theo trang thai, top dia ban, chart tiep nhan/giai quyet.
- Metric cards co so sanh voi ky truoc:
  - `Nam nay`: so voi cung khoang nam truoc.
  - `Thang nay`: so voi thang truoc.
  - `Tuan nay`: so voi tuan truoc.
  - Custom range: so voi khoang lien ke truoc do, cung so ngay inclusive.
- Tren metric cards, mui ten va so delta in dam, cung mau xanh/do/xam; phan chu mo ta dung mau chu thuong cua app.
- Donut animation da fix nhay/flash: khong fade ve 0, co throttle.
- Mouse wheel trong chart van scroll page.

### 2. Nhap du lieu

Dung khi user noi `Nhap du lieu`, `form ho so`, `ma ho so`, `file dinh kem`, `xoa/luu ho so`.

File can doc:
- `ViewModels\RecordInputViewModel.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `Models\RecordModels.cs`
- Service methods: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, `DeleteRecord`

Da lam:
- Ma ho so tu sinh dang `HS-{yyyy}-{000000}` va readonly tren UI.
- Form tao moi de trong, khong tu do ho so mau.
- Kiem tra field bat buoc truoc khi luu.
- Canh bao ho so co kha nang trung theo nguoi gui, dia ban, loai vu viec, ngay +/-30 ngay, va so dien thoai neu co.
- Tai lieu dinh kem co chon file, drag/drop, luu `FilePath`.

### 3. Danh sach ho so

Dung khi user noi `Danh sach ho so`, `bo loc danh sach`, `xuat excel`, `phan loai trong thao tac`, `copy bang`, `icon thao tac`.

File can doc:
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordListRowViewModel.cs`
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- Service methods: `GetFilteredRecords`, `CountFilteredRecords`, `GetExportPreview`, `DeleteRecord`, `GetRecordForm`

Da lam:
- Da tich hop bo loc va nut `Xuat Excel` vao trang danh sach.
- Mac dinh export file Excel `.xlsx`.
- Page `Xuat du lieu` da bi xoa khoi navigation/template.
- Header trang danh sach da bo nut `Quay lai` va `Lam moi`.
- Bang co phan trang, chon so dong/trang.
- Cot thao tac co icon xem/sua/phan loai/xoa, mau dong bo, khong in dam.
- Da chong highlight ca cum icon khi click.
- Copy bang chi copy vung chon, khong copy header.
- Nut `Phan loai` trong thao tac mo chi tiet xu ly va giu sidebar highlight `Danh sach ho so`.

### 4. Phan loai & xu ly

Dung khi user noi `Phan loai`, `xu ly`, `queue`, `card loc`, `timeline`, `trang thai xu ly`.

File can doc:
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordProcessingView.xaml`
- `Views\Records\RecordProcessingView.xaml.cs`
- `Models\RecordModels.cs`
- Service methods: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, `CountProcessingQueueRecords`, `GetProcessingRecordDetail`, `UpdateProcessingRecord`

Da lam:
- Trang chinh la danh sach ho so can xu ly, co cards loc thay cho bo loc cu.
- Click card se loc bang theo card.
- Cards hien co: `All`, `NeedClassify`, `Processing`, `Waiting`, `DueSoon`, `Overdue`, `HighPriority`.
- Bang queue co phan trang that, khong gioi han toi da 20 trang.
- Mouse wheel tren bang van scroll page.
- Chi tiet xu ly co timeline 7 buoc va form cap nhat trang thai.
- O `Nguoi xu ly` la combobox editable, lay danh sach tu catalog `ProcessorName` va ten can bo tung co trong `Records`.
- Quay lai tu chi tiet ve dung trang nguon. Neu vao tu `Danh sach ho so` thi sidebar van highlight `Danh sach ho so`.

### 5. Cai dat

Dung khi user noi `Cai dat`, `danh muc`, `quan ly popup`, `nhat ky he thong`, `backup`, `cap nhat phan mem`.

File can doc:
- `ViewModels\SettingsViewModel.cs`
- `Views\Settings\SettingsView.xaml`
- `Views\Settings\SettingsView.xaml.cs`
- `Models\SettingsModels.cs`
- Service methods: catalog methods va system log methods trong `AppDataService.cs`

Da lam gan nhat:
- Trang cai dat bo nut `Lam moi`.
- `Danh muc he thong` hien card co icon trong vong tron nen nhat. Click vao card de mo popup, khong co nut `Quan ly` rieng.
- Card danh muc chi highlight khi hover, khong highlight mac dinh va khong giu highlight sau click.
- Danh muc hien co: `Nguon tiep nhan`, `Loai vu viec`, `Linh vuc`, `Nhom noi dung`, `Muc do uu tien`, `Ten can bo xu ly`, `Huong xu ly`.
- Popup danh muc da bo filter, bo xem trang thai, bo phan trang. Chi can them moi, sua, xoa danh muc hien co.
- Trong popup danh muc, nut sua/xoa la icon. Bam sua se dua gia tri len o `Danh muc hien tai`; bam `Luu thay doi` de luu. Danh sach co keo tha len/xuong de doi thu tu hien thi va luu order.
- `Thao tac nhanh` xep doc, gom `Nhat ky he thong`, `Cai dat chung`, `Huong dan`.
- `Cai dat chung` cho doi duong dan DB va thu muc log. DB path luu trong `%LocalAppData%\QuanLyHoSo\Settings\path-settings.json`; doi DB path can restart app.
- `Huong dan` mo popup huong dan ngan cho cac phan trong trang cai dat.
- `Thong tin phan mem` hien theo thu tu: phien ban, moi truong chay, loai co so du lieu, dung luong du lieu, don vi phat trien `minhthang3321@gmail.com`.
- `Sao luu du lieu` da chong overlap dong `Chua co du lieu sao luu` khi man hinh co lai.
- Click `Nhat ky he thong` mo popup danh sach audit log.

### 6. Page xuat du lieu da bo

Khong doc/sua cac file nay vi da xoa:
- `ViewModels\ExportViewModel.cs`
- `Views\Export\ExportView.xaml`
- `Views\Export\ExportView.xaml.cs`

Neu user hoi export, xu ly trong:
- `RecordListViewModel.cs`
- `RecordListView.xaml`

## Database/schema

Bang chinh:
- `Areas`: dia ban.
- `CatalogItems`: danh muc dung cho combobox/filter.
- Catalog `ProcessorName`: ten can bo xu ly; duoc sync tu `Records.ProcessorName` va cap nhat khi luu xu ly ho so.
- `Records`: ho so.
- `RecordAttachments`: file dinh kem cua ho so.
- `ProcessHistories`: lich su/timeline xu ly ho so.
- `SystemLogs`: audit log thao tac anh huong database.

Audit log:
- Model UI: `SystemLogEntry` trong `Models\SettingsModels.cs`.
- Doc log: `AppDataService.GetSystemLogs(int take = 200)`.
- Ghi log: helper `WriteDatabaseLog(...)` trong `AppDataService.cs`.
- Dang ghi cho:
  - Them/sua/xoa/sap xep danh muc.
  - Them/sua/xoa ho so.
  - Cap nhat xu ly/trang thai ho so.
- Khong log thao tac doc du lieu, filter, xem chi tiet, export file, backup file, refresh UI.
- Khong log seed/init de tranh nhat ky bi day boi du lieu tu dong khi mo app.

## Presentation helpers

- `Presentation\DataGridCopyBehavior.cs`: Ctrl+C cho DataGrid, chi copy vung chon, khong copy header.
- `Presentation\BindingProxy.cs`: dung khi can bind trong `DataGridColumn`.
- `Presentation\Converters\StatusToBrushConverter.cs`: mau badge trang thai.
- `Presentation\Converters\StatusDonutSegmentConverter.cs`: ve lat donut chart.
- `Presentation\Converters\BooleanToNavBrushConverter.cs`: mau sidebar/nav.

## Navigation/highlight can nho

Flow mong muon cua user:

```text
Dung o Danh sach ho so
-> sidebar highlight Danh sach ho so
-> bam Chi tiet/Phan loai ho so
-> vao trang chi tiet/xu ly nhung sidebar van highlight Danh sach ho so
-> Back
-> ve Danh sach ho so va sidebar van highlight Danh sach ho so
```

Lien quan:
- `ShellViewModel.NavigateTo(key, selectedNavigationKey)`
- `ShellViewModel.ClassifyRecordFromList(...)`
- `RecordProcessingViewModel.OpenRecord(recordCode, returnToPreviousPage: true)`
- `RecordProcessingViewModel.BackToQueue()`

## Build/verify

Lenh verify nen dung:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-build
```

Ly do: build mac dinh co the fail neu app dang mo va khoa file exe trong `bin/Debug/net5.0-windows`.

Ket qua gan nhat:
- Build thanh cong.
- 0 errors.
- Warning con lai: `NETSDK1138` do `net5.0-windows` het support.

## Git status gan nhat

Working tree dang co nhieu thay doi chua commit, gom cac nhom:
- Xoa page xuat du lieu.
- Tich hop export vao danh sach ho so.
- Cap nhat dashboard animation/chart va delta so sanh voi ky truoc.
- Cap nhat danh sach ho so va phan loai/xu ly.
- Them copy behavior DataGrid.
- Cap nhat trang cai dat popup danh muc, thong tin phan mem, cai dat chung, huong dan, backup UI va nhat ky he thong.
- Them audit log DB.
- Them cau hinh path DB/log trong `Infrastructure\Configuration\AppPathSettings.cs`.
- Co thay doi script/release/installer/assets.

Truoc khi commit hay revert, phai xem `git diff --stat` va `git diff <file>`; khong revert cac thay doi khong lien quan.

## Cach tiep can task moi

- Task UI trang nao: mo XAML + ViewModel cua trang do, sau do moi mo service neu binding can du lieu.
- Task query/du lieu: tim method service bang `rg`, mo dung method va model lien quan.
- Task style chung: mo `App.xaml`; can than vi anh huong toan app.
- Task table/copy/icon: mo XAML cua trang + `Presentation\DataGridCopyBehavior.cs` neu lien quan copy.
- Task navigation/back/sidebar: mo `ShellViewModel.cs` va ViewModel trang nguon/dich.
- Task release/build: mo `QuanLyHoSo.csproj`, `.github\workflows\release.yml`, `scripts\build-release.ps1`, `installer\QuanLyHoSo.iss`.

## Viec vua lam trong request gan nhat

1. Trang tong quan:
   - Metric cards co delta so voi ky truoc theo filter thoi gian.
   - Mui ten va so delta in dam/cung mau; phan chu mo ta dung mau chu thuong.
2. Trang cai dat:
   - Bo nut `Lam moi`.
   - Danh muc he thong dung card click-to-open, hover highlight, popup gon lai de them/sua/xoa/keo tha sap xep.
   - Them catalog `Ten can bo xu ly`, lay va sync voi nguoi xu ly trong ho so.
   - Thao tac nhanh xep doc, them `Cai dat chung` va `Huong dan`.
   - Thong tin phan mem chi giu cac muc can thiet theo thu tu moi.
3. Trang danh sach ho so:
   - Bo nut `Quay lai` va `Lam moi` tren header.

## Cap nhat 2026-08-31 - Safe finding fixes

User yeu cau: fix tat ca finding co the fix, nhung khong doi business logic va khong doi GUI.

Da lam:
- `ViewModels\DashboardViewModel.cs`
  - `Reload()` da load lai bang ho so gan day va tinh `TotalRecentPages`.
  - Fix tinh trang section recent records co the trong cho toi khi user bam phan trang.
- `ViewModels\RecordListViewModel.cs`
  - Export Excel chuyen sang async/background cho buoc load data va ghi `.xlsx`.
  - Them `_isExporting` va raise `CanExecuteChanged` de tranh bam export lap khi dang chay.
  - Them catch/log rieng khi load du lieu export loi, tranh roi vao global exception handler.
  - UI/filter/output format/message ve co ban giu nhu cu.
- `Infrastructure\Data\AppDataService.cs`
  - Them `BackupDatabase(...)` dung SQLite `BackupDatabase` API thay vi `File.Copy` DB song.
  - Them `RestoreDatabaseFromFile(...)`: validate file backup, tao safety backup truoc restore, restore bang SQLite backup API, `PRAGMA quick_check` sau restore.
  - Them `ValidateDatabaseFile(...)` va helper validate connection bang `PRAGMA quick_check`.
  - Bo dead/unreachable fallback trong `GetProcessingQueueMetrics()` (`metrics.Count >= 0`).
  - Them `CreateIndexes(...)` voi `CREATE INDEX IF NOT EXISTS` cho cac query hien co: ngay tiep nhan, updated/status/area/case type/field/processor/priority, attachment/history theo `RecordId`, catalog, system logs.
- `ViewModels\SettingsViewModel.cs`
  - `BackupNowCommand` va `RestoreDataCommand` chuyen sang async command.
  - Backup button van dung flow cu nhung copy DB an toan hon.
  - Restore button khong con placeholder: cho chon file `.db`, confirm, tao safety backup, restore, refresh thong tin/counter.
  - Khi doi DB path trong cai dat chung, copy DB ban dau cung dung backup API.
- `ViewModels\ShellViewModel.cs`
  - Page ViewModel duoc lazy-create theo navigation.
  - Startup khong con khoi tao san tat ca page ViewModel, giam chi phi mo app.
  - Navigation/workflow/sidebar highlight giu nguyen.
- `App.xaml`
  - DataGrid style chung bat `EnableRowVirtualization`, `EnableColumnVirtualization`, `VirtualizingPanel.IsVirtualizing`, `VirtualizationMode=Recycling`.
- `Models\SettingsModels.cs`
  - `CatalogValueSetting` co them `IsActive` de data service co the doc trang thai danh muc khi can.

Khong tu sua vi se doi business logic/data migration/UX:
- Hard delete ho so sang soft delete.
- Ep rule/policy chuyen trang thai ho so.
- Copy attachment vao managed app storage thay vi luu path goc.
- Nang target framework khoi `.NET 5.0-windows`.
- Tach lon `AppDataService` thanh nhieu repository/service. Nen lam rieng theo tung PR nho neu user approve.

Verify gan nhat:
- `dotnet build QuanLyHoSo.sln -p:UseAppHost=false`
- Ket qua: build thanh cong, 0 errors.
- Warning con lai: `NETSDK1138` do `.NET 5.0-windows` het support.
- Build mac dinh `dotnet build QuanLyHoSo.sln` co the fail neu app dang mo va khoa `bin\Debug\net5.0-windows\QuanLyHoSo.exe` (da gap process `QuanLyHoSo (33316)`).
