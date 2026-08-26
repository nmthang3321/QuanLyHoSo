# Thiet ke he thong phan mem Quan ly ho so

## 1. Muc tieu thiet ke

Tai lieu nay de xuat thiet ke he thong cho phan mem **Quan ly ho so** chay tren Windows Desktop bang **WPF .NET 5.0**. Thiet ke duoc xay dung dua tren:

- Proposal: `doc/proposal/Proposal_Phan_Mem_Quan_Ly_Ho_So_An_Giang_Updated.pdf`
- GUI draft: `doc/GUI/tong_quan.png`, `doc/GUI/nhap_du_lieu.png`, `doc/GUI/phan_loai_xu_ly.png`, `doc/GUI/xuat_du_lieu.png`, `doc/GUI/cai_dat.png`

Muc tieu chinh:

- De phat trien theo tung man hinh va tung nghiep vu ro rang.
- De bao tri, de them truong du lieu/danh muc/trang thai moi.
- De trace log khi co issue: biet thao tac nao, ho so nao, nguoi xu ly nao, loi o lop nao.
- Tach biet UI, nghiep vu, truy cap du lieu va ha tang ky thuat.
- Phu hop pham vi local desktop: khong server, khong dong bo Internet, khong workflow phe duyet nhieu cap.

## 2. Pham vi chuc nang

### 2.1 Bao gom

- Tong quan/thong ke ho so theo thoi gian, trang thai, dia ban.
- Nhap, xem, sua, xoa ho so.
- Quan ly file dinh kem: PDF, JPG, PNG, toi da 10 MB/file.
- Phan loai va xu ly ho so theo quy trinh.
- Luu lich su xu ly theo tung lan cap nhat.
- Tim kiem, loc va sap xep ho so.
- Xuat du lieu Excel `.xlsx` va CSV `.csv`.
- Quan ly danh muc dia ban, loai vu viec, linh vuc, nhom noi dung, nguon tiep nhan.
- Sao luu va khoi phuc du lieu local.

### 2.2 Khong bao gom trong giai do dau

- Multi-user dong thoi qua server tap trung.
- Phan quyen nhieu cap phuc tap.
- Web/mobile app.
- Chu ky so, email/SMS tu dong, tich hop he thong ngoai.
- He thong DMS chuyen sau hoac dashboard phan tich nang cao.

## 3. Nguyen tac kien truc

He thong nen dung **MVVM ket hop Clean Architecture nhe**. WPF phu hop voi MVVM, con Clean Architecture giup tach nghiep vu khoi UI va database.

Luon giu mot chieu phu thuoc:

```text
UI WPF
  -> ViewModels
    -> Application Services / Use Cases
      -> Domain
      -> Repository Interfaces
        -> Infrastructure Implementations
          -> SQLite / File System / Excel / Logging
```

Quy tac:

- View chi hien thi va bind command, khong viet nghiep vu trong code-behind.
- ViewModel dieu phoi man hinh, validate dau vao gan UI, goi use case.
- Application layer chua luong nghiep vu cap ung dung.
- Domain layer chua entity, enum, business rule, state transition.
- Infrastructure layer xu ly SQLite, file dinh kem, export, backup, logging.

## 4. De xuat cau truc project

Co the bat dau bang mot project WPF duy nhat de don gian, nhung nen chia folder theo layer. Khi he thong lon hon co the tach thanh nhieu project class library.

```text
QuanLyHoSo/
  App.xaml
  App.xaml.cs
  MainWindow.xaml
  MainWindow.xaml.cs

  Views/
    Shell/
      ShellWindow.xaml
      SidebarView.xaml
    Dashboard/
      DashboardView.xaml
    Records/
      RecordInputView.xaml
      RecordListView.xaml
      RecordDetailView.xaml
      RecordProcessingView.xaml
    Export/
      ExportView.xaml
    Settings/
      SettingsView.xaml

  ViewModels/
    ShellViewModel.cs
    DashboardViewModel.cs
    RecordInputViewModel.cs
    RecordListViewModel.cs
    RecordProcessingViewModel.cs
    ExportViewModel.cs
    SettingsViewModel.cs

  Application/
    Common/
      Result.cs
      PagedResult.cs
      DateRange.cs
    Records/
      CreateRecordUseCase.cs
      UpdateRecordUseCase.cs
      DeleteRecordUseCase.cs
      SearchRecordsUseCase.cs
      UpdateRecordProcessUseCase.cs
    Dashboard/
      GetDashboardSummaryUseCase.cs
    Exporting/
      ExportRecordsUseCase.cs
    Settings/
      ManageCatalogUseCase.cs
      BackupDatabaseUseCase.cs
      RestoreDatabaseUseCase.cs

  Domain/
    Entities/
      Record.cs
      RecordAttachment.cs
      RecordProcessHistory.cs
      CatalogItem.cs
      Area.cs
      UserProfile.cs
    Enums/
      RecordStatus.cs
      ProcessingStep.cs
      PriorityLevel.cs
      SeverityLevel.cs
      ReceiveSource.cs
    Rules/
      RecordStatusTransitionPolicy.cs
      AttachmentPolicy.cs

  Infrastructure/
    Data/
      AppDbContext.cs
      Migrations/
      Repositories/
    FileStorage/
      AttachmentStorageService.cs
    Exporting/
      ExcelExportService.cs
      CsvExportService.cs
    Backup/
      BackupService.cs
    Logging/
      LoggingSetup.cs
      AuditLogger.cs

  Resources/
    Styles/
    Icons/
    Templates/

  Config/
    appsettings.json
```

## 5. Module man hinh

### 5.1 Shell va dieu huong

Shell la khung chinh cua ung dung gom sidebar va vung noi dung. Sidebar theo GUI draft gom:

- Tong quan
- Nhap du lieu
- Phan loai & Xu ly
- Xuat du lieu
- Cai dat

Nen dung `ShellViewModel` giu `CurrentViewModel`. Moi nut sidebar chay command doi view model hien tai.

### 5.2 Tong quan

Muc dich: giup nguoi quan ly nam tinh hinh tiep nhan va xu ly ho so.

Du lieu can hien thi:

- Tong ho so.
- Dang xu ly.
- Da giai quyet.
- Cho ket qua.
- Ho so theo trang thai.
- Top 5 dia ban co nhieu ho so.
- Ho so cap nhat gan day.

Use case chinh:

- `GetDashboardSummaryUseCase`
- `GetRecordStatusChartUseCase`
- `GetTopAreasUseCase`
- `GetRecentRecordsUseCase`

Truy van dashboard nen la read-only, toi uu bang projection DTO thay vi load full entity.

### 5.3 Nhap du lieu

Muc dich: tiep nhan va luu thong tin ban dau cua ho so.

Nhom thong tin:

- Thong tin chung: ma ho so, ngay tiep nhan, nguon tiep nhan, nguoi tiep nhan.
- Nguoi gui/to chuc: ten, so dien thoai, dia chi lien he.
- Dia ban va noi dung: dia ban, dia chi xay ra vu viec, noi dung don/vu viec.
- Thong tin nghiep vu: loai vu viec, nhom noi dung, linh vuc, doi tuong lien quan.
- Thong tin bo sung: hinh thuc xu ly mong muon, muc do vu viec, ngay hen tra ket qua, uu tien.
- Tai lieu dinh kem.

Nguyen tac:

- Ma ho so nen sinh theo format thong nhat, vi du `HS-yyyy-000001`.
- Validate bat buoc tai ViewModel va Application layer.
- File dinh kem khong nen luu binary truc tiep vao DB; nen copy vao thu muc ung dung va DB chi luu metadata/path tuong doi.
- Khi tao ho so thanh cong, tu dong tao mot dong lich su xu ly dau tien: `TiepNhan`.

### 5.4 Phan loai & Xu ly ho so

Quy trinh proposal:

```text
Tiep nhan -> Phan loai -> Phan cong -> Xac minh -> Gia han (neu co) -> Ket thuc -> Luu ho so
```

Trang thai ho so:

- Moi tiep nhan
- Dang phan loai
- Da phan cong
- Dang xac minh
- Cho ket qua
- Dang cho bo sung tai lieu
- Da giai quyet
- Chuyen co quan khac

Nen thiet ke `RecordStatusTransitionPolicy` de kiem soat chuyen trang thai hop le. Vi du:

```text
MoiTiepNhan -> DangPhanLoai
DangPhanLoai -> DaPhanCong | ChuyenCoQuanKhac
DaPhanCong -> DangXacMinh
DangXacMinh -> ChoKetQua | DangChoBoSungTaiLieu | DaGiaiQuyet
ChoKetQua -> DaGiaiQuyet
DangChoBoSungTaiLieu -> DangXacMinh | DaGiaiQuyet
```

Moi lan cap nhat xu ly phai ghi:

- Ho so nao.
- Trang thai cu.
- Trang thai moi.
- Buoc xu ly.
- Ngay gio xu ly.
- Nguoi xu ly.
- Noi dung xu ly.
- Ghi chu.
- CorrelationId cua thao tac.

### 5.5 Xuat du lieu

Bo loc:

- Khoang ngay tiep nhan.
- Trang thai ho so.
- Loai vu viec.
- Linh vuc.
- Dia ban/xa/phuong.
- Nguoi xu ly.
- Tu khoa.
- Sap xep.

Luong xu ly:

```text
Nguoi dung chon bo loc
  -> SearchRecordsUseCase tra preview
  -> ExportRecordsUseCase dung cung filter
  -> ExcelExportService hoac CsvExportService tao file
  -> Ghi audit/log ket qua xuat
```

Nen tao `RecordSearchCriteria` dung chung cho preview va export de tranh lech ket qua.

### 5.6 Cai dat

Nhom chuc nang:

- Danh muc dia ban: 102 xa/phuong tinh An Giang.
- Danh muc nghiep vu: loai vu viec, linh vuc, nhom noi dung, nguon tiep nhan.
- Sao luu va khoi phuc du lieu.
- Thong tin phan mem.

Danh muc nen co co che `IsActive` thay vi xoa vat ly ngay lap tuc. Neu danh muc da duoc gan vao ho so, thao tac xoa nen chuyen sang vo hieu hoa de giu toan ven lich su.

## 6. Mo hinh du lieu de xuat

### 6.1 Record

`Record` la aggregate root cua nghiep vu ho so.

Truong chinh:

- `Id`
- `RecordCode`
- `ReceivedDate`
- `ReceiveSourceId`
- `ReceiverName`
- `SenderName`
- `SenderPhone`
- `ContactAddress`
- `AreaId`
- `IncidentAddress`
- `Content`
- `CaseTypeId`
- `ContentGroupId`
- `FieldId`
- `RelatedPerson`
- `ExpectedHandlingMethod`
- `SeverityLevel`
- `ExpectedResultDate`
- `PriorityLevel`
- `CurrentStatus`
- `CurrentProcessingStep`
- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`
- `IsDeleted`

### 6.2 RecordAttachment

Truong chinh:

- `Id`
- `RecordId`
- `OriginalFileName`
- `StoredFileName`
- `RelativePath`
- `ContentType`
- `FileExtension`
- `FileSizeBytes`
- `Sha256Hash`
- `UploadedAt`
- `UploadedBy`
- `IsDeleted`

Luu y:

- Gioi han 10 MB/file.
- Chi chap nhan `.pdf`, `.jpg`, `.jpeg`, `.png`.
- Nen tinh hash de phat hien trung file va ho tro trace.

### 6.3 RecordProcessHistory

Truong chinh:

- `Id`
- `RecordId`
- `FromStatus`
- `ToStatus`
- `ProcessingStep`
- `ProcessedAt`
- `ProcessorName`
- `ActionContent`
- `Note`
- `CorrelationId`
- `CreatedAt`

Bang nay la nguon chinh de trace tien trinh xu ly cua ho so.

### 6.4 CatalogItem

Dung chung cho cac danh muc nghiep vu.

Truong chinh:

- `Id`
- `CatalogType`
- `Code`
- `Name`
- `DisplayOrder`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`

`CatalogType` co the gom:

- `CaseType`
- `Field`
- `ContentGroup`
- `ReceiveSource`
- `ExpectedHandlingMethod`

### 6.5 Area

Truong chinh:

- `Id`
- `Code`
- `Name`
- `DistrictName`
- `ProvinceName`
- `DisplayOrder`
- `IsActive`

Ban dau import 102 xa/phuong An Giang.

### 6.6 AuditLog

Ngoai technical log, nen co bang audit de xem lai thao tac nghiep vu quan trong.

Truong chinh:

- `Id`
- `OccurredAt`
- `UserName`
- `Action`
- `EntityType`
- `EntityId`
- `EntityCode`
- `Summary`
- `CorrelationId`

Hanh dong nen audit:

- Tao/sua/xoa ho so.
- Them/xoa file dinh kem.
- Cap nhat trang thai xu ly.
- Xuat file.
- Sao luu/khoi phuc du lieu.
- Them/sua/xoa danh muc.

## 7. Co so du lieu

De xuat dung **SQLite local** vi phu hop proposal: ung dung desktop, du lieu tap trung trong may, khong server.

Thu muc du lieu de xuat:

```text
%ProgramData%/QuanLyHoSo/
  Data/
    quanlyhoso.db
  Attachments/
    2026/
      08/
        HS-2026-000125/
  Backups/
  Logs/
```

Neu can chay khong can quyen admin, co the dung:

```text
%LocalAppData%/QuanLyHoSo/
```

Khuyen nghi thu vien:

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Configuration.Json`
- `Serilog`
- `Serilog.Sinks.File`
- `ClosedXML` cho Excel
- `CsvHelper` cho CSV

## 8. Logging va trace issue

### 8.1 Nguyen tac log

Log phai giup tra loi nhanh:

- Loi xay ra o thao tac nao?
- Ho so lien quan la ho so nao?
- Nguoi dung dang lam gi?
- Du lieu dau vao co gi bat thuong?
- Loi o UI, nghiep vu, database, file system hay export?

Moi command/use case nen tao `CorrelationId` va day xuong cac layer.

Format log de xuat:

```text
Timestamp | Level | CorrelationId | User | Module | Action | RecordCode | Message | Exception
```

### 8.2 Vi tri log

```text
%ProgramData%/QuanLyHoSo/Logs/
  app-2026-08-26.log
  error-2026-08-26.log
```

### 8.3 Logging theo layer

ViewModel:

- Log command start/end o muc Information.
- Log validation fail o muc Warning.

Application:

- Log business decision quan trong.
- Log state transition.
- Log ket qua use case.

Infrastructure:

- Log database exception.
- Log file copy/delete/export/backup.
- Log duration cac tac vu cham.

### 8.4 Exception handling

Can co `GlobalExceptionHandler` trong `App.xaml.cs`:

- Bat `DispatcherUnhandledException`.
- Bat `TaskScheduler.UnobservedTaskException`.
- Ghi log error voi stack trace.
- Hien dialog than thien: "Da co loi xay ra. Ma tra cuu: {CorrelationId}".

Khong hien stack trace cho nguoi dung cuoi.

## 9. Validation va business rule

Validation nen chia 2 lop:

- UI/ViewModel validation: bat buoc nhap, format so dien thoai, ngay hop le, gioi han ky tu.
- Domain/Application validation: trang thai hop le, danh muc active, file hop le, ho so ton tai.

Rule quan trong:

- `RecordCode` khong trung.
- `ReceivedDate` khong duoc lon hon ngay hien tai qua xa neu khong co ly do.
- `ExpectedResultDate` phai >= `ReceivedDate`.
- File dinh kem toi da 10 MB/file.
- Khong xoa vat ly ho so da co lich su xu ly; dung soft delete.
- Khong cho chuyen trang thai neu transition khong hop le.

## 10. Bao mat va an toan du lieu

Voi pham vi local desktop, bao mat tap trung vao du lieu tren may:

- Khong luu password/token trong source code.
- Thu muc data/backup/log nen dat tai vi tri ro rang, co the cau hinh.
- Backup nen tao file nen `.zip` gom SQLite DB va attachments.
- Truoc khi restore phai tu dong tao backup hien trang.
- File dinh kem copy vao vung quan ly cua app, khong chi link den file goc cua nguoi dung.
- Log khong nen ghi toan bo noi dung don neu noi dung co the nhay cam; chi ghi ma ho so va tom tat.

## 11. Chien luoc sao luu va khoi phuc

### 11.1 Backup

Luong backup:

```text
Khoa thao tac ghi ngan han
  -> Tao checkpoint SQLite
  -> Copy DB vao thu muc tam
  -> Copy attachments
  -> Nen thanh file zip
  -> Ghi AuditLog BackupCompleted
```

Ten file:

```text
QuanLyHoSo_Backup_yyyyMMdd_HHmmss.zip
```

### 11.2 Restore

Luong restore:

```text
Nguoi dung chon file backup
  -> Validate cau truc zip
  -> Tao backup hien trang
  -> Dong ket noi DB
  -> Restore DB va attachments
  -> Mo lai ung dung hoac reload app
  -> Ghi AuditLog RestoreCompleted
```

## 12. Thiet ke OOP trong C#

### 12.1 Entity

Entity nen bao ve invariant, khong chi la class public set tran lan. Vi du `Record` co method:

```csharp
public void UpdateStatus(
    RecordStatus newStatus,
    ProcessingStep step,
    string processorName,
    string actionContent,
    string note,
    IRecordStatusTransitionPolicy transitionPolicy,
    Guid correlationId)
```

Method nay:

- Kiem tra transition hop le.
- Cap nhat `CurrentStatus`.
- Tao `RecordProcessHistory`.
- Cap nhat `UpdatedAt`.

### 12.2 Service

Service chi nen lam viec co tinh nghiep vu/ha tang ro rang:

- `RecordNumberGenerator`
- `AttachmentStorageService`
- `DashboardQueryService`
- `ExportService`
- `BackupService`
- `AuditLogger`

### 12.3 Interface

Application layer phu thuoc interface:

```csharp
public interface IRecordRepository
{
    Task<Record?> GetByIdAsync(Guid id);
    Task<Record?> GetByCodeAsync(string recordCode);
    Task AddAsync(Record record);
    Task SaveChangesAsync();
}
```

Infrastructure implement interface bang EF Core/SQLite.

## 13. MVVM va UI maintainability

Nen dung `CommunityToolkit.Mvvm` de giam boilerplate:

- `ObservableObject`
- `RelayCommand`
- `AsyncRelayCommand`

Quy tac UI:

- Moi man hinh co mot ViewModel rieng.
- Command async phai co loading state va error state.
- Control dung chung nen dua vao `Resources/Styles`.
- ComboBox danh muc load tu `CatalogLookupService`.
- Bang danh sach nen dung DTO rieng, khong bind truc tiep entity EF.

Vi du ViewModel mapping:

```text
DashboardView.xaml          -> DashboardViewModel
RecordInputView.xaml        -> RecordInputViewModel
RecordProcessingView.xaml   -> RecordProcessingViewModel
ExportView.xaml             -> ExportViewModel
SettingsView.xaml           -> SettingsViewModel
```

## 14. Goi y package NuGet

```text
CommunityToolkit.Mvvm
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Configuration
Microsoft.Extensions.Configuration.Json
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Design
Serilog
Serilog.Extensions.Hosting
Serilog.Sinks.File
ClosedXML
CsvHelper
```

Neu muon ve chart trong dashboard:

```text
LiveChartsCore.SkiaSharpView.WPF
```

## 15. Luong nghiep vu chinh

### 15.1 Tao ho so

```text
RecordInputViewModel.SaveCommand
  -> Validate form
  -> CreateRecordUseCase
  -> RecordNumberGenerator sinh ma
  -> AttachmentStorageService copy file
  -> RecordRepository.Add
  -> AuditLogger ghi CreatedRecord
  -> Logger ghi Information voi CorrelationId
```

### 15.2 Cap nhat xu ly

```text
RecordProcessingViewModel.UpdateCommand
  -> Validate trang thai va noi dung
  -> UpdateRecordProcessUseCase
  -> RecordStatusTransitionPolicy kiem tra
  -> Record.UpdateStatus
  -> RecordRepository.SaveChanges
  -> AuditLogger ghi StatusChanged
```

### 15.3 Xuat du lieu

```text
ExportViewModel.ExportCommand
  -> Build RecordSearchCriteria
  -> SearchRecordsUseCase lay dung tap du lieu
  -> ExportRecordsUseCase
  -> ExcelExportService/CsvExportService
  -> AuditLogger ghi ExportedRecords
```

## 16. Test strategy

Nen co test cho nghiep vu quan trong, du app desktop van nen test core logic.

Unit test:

- Sinh ma ho so.
- Transition trang thai.
- Validate file dinh kem.
- Search criteria mapping.
- Export column mapping.

Integration test:

- Repository voi SQLite in-memory.
- Tao ho so kem lich su tiep nhan.
- Cap nhat trang thai tao lich su xu ly.
- Backup/restore voi thu muc tam.

Manual test:

- Tao ho so day du thong tin.
- Sua ho so co file dinh kem.
- Xoa file dinh kem.
- Loc dashboard theo thang.
- Xuat Excel/CSV theo filter.
- Restore tu backup.

## 17. Lo trinh trien khai de xuat

### Giai doan 1: Nen mong ky thuat

- Setup DI, logging, config.
- Setup SQLite + EF Core.
- Tao entity, enum, repository.
- Tao shell/sidebar va routing man hinh.

### Giai doan 2: Ho so va danh muc

- Danh muc dia ban/nghiep vu.
- Nhap ho so.
- Danh sach va chi tiet ho so.
- File dinh kem.

### Giai doan 3: Xu ly va lich su

- Phan loai & xu ly.
- State transition policy.
- Timeline lich su xu ly.
- Audit log.

### Giai doan 4: Bao cao va van hanh

- Dashboard.
- Xuat Excel/CSV.
- Sao luu/khoi phuc.
- Dong goi installer va huong dan su dung.

## 18. Rui ro va cach kiem soat

| Rui ro | Anh huong | Cach kiem soat |
| --- | --- | --- |
| Thay doi danh muc nghiep vu | Anh huong form, thong ke, export | Dung `CatalogItem`, khong hard-code danh muc trong UI |
| File dinh kem bi mat/di chuyen | Ho so mat tai lieu | Copy file vao thu muc app, DB luu path tuong doi va hash |
| Loi kho trace khi nguoi dung bao issue | Ton thoi gian debug | Bat buoc log `CorrelationId`, `RecordCode`, `Action` |
| DB local bi hong | Mat du lieu | Backup dinh ky, backup truoc restore |
| UI code-behind phinh to | Kho maintain | Ap dung MVVM, command/use case rieng |
| Xoa danh muc da duoc su dung | Mat y nghia lich su | Dung soft delete/IsActive |

## 19. Ket luan

Thiet ke de xuat tap trung vao mot ung dung WPF local gon, ro layer va de mo rong. Phan quan trong nhat la tach nghiep vu ho so ra khoi UI, thiet ke state transition co kiem soat, luu lich su xu ly day du va co logging/audit theo `CorrelationId`. Cach lam nay giup ung dung de maintain, de trace issue va phu hop voi pham vi proposal hien tai.
