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
- Tai lieu lien quan ho tro PDF/Word/anh. Chi khi save o step 5 va attachment chua du 3 phieu moi hoi tao file. Neu chon Yes, `InitialResultDocumentGenerator` chi tao phieu con thieu tu template Word trong `doc\`, append vao attachment va refresh detail; khong hien progress. Kiem tra ten file khong phan biet hoa/thuong, ke ca hau to timestamp do generator tao.
- Khi mo lai chi tiet xu ly, sau khi nap `Attachments` phai notify `HasAttachments`; neu thieu notify thi file van co trong DB/popup xem ho so nhung danh sach tai lieu xu ly bi giu `Collapsed`.
- Khi trang thai la `Chuyen co quan khac`, selector `Co quan chuyen den` phai dung cung overlay/group/search/display nhu selector `Dia ban` o trang Nhap du lieu; khong doi lai thanh ComboBox phang.
