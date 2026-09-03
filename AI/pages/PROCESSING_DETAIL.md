# Page - Phan loai & xu ly detail/update

Dung khi task lien quan mo chi tiet xu ly, cap nhat trang thai/lich su.

Files:
- `Views\Records\RecordProcessingView.xaml`
- `ViewModels\RecordProcessingViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetProcessingRecordDetail`
- `UpdateProcessingRecord`

Notes:
- `ProcessingRecordDetail` nam trong `Models\RecordModels.cs`.
- Navigation/back/sidebar highlight lien quan `ShellViewModel`.
- Role: Officer chi sua ho so minh phu trach, Leader chi xem.
- Rule quy trinh: Officer khong duoc chon/cap nhat ve `Moi tiep nhan` hoac `Dang phan loai`; sau `Da phan cong` co the quan ly cac buoc 3-7. Admin khong bi gioi han buoc.
- Tai lieu lien quan ho tro PDF/Word/anh. Khi save den step 5 tro di va user chon Yes trong popup, `InitialResultDocumentGenerator` copy template Word `.docx` trong `doc\` roi thay cac vung highlight bang du lieu ho so, luu thanh `.docx`, append vao attachment va refresh detail sau progress.
