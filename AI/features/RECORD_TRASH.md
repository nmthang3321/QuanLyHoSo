# Feature - Thung rac ho so

- Admin: Danh sach ho so -> Thung rac hien popup overlay ngay trong trang (khong mo Window moi). Tim theo ma, nguoi gui, nguoi xoa; chon tung dong/nhieu/tat ca ket qua va Khoi phuc da chon. Dong popup se reload danh sach; khong dong khi dang tai/khoi phuc.
- Xoa o danh sach/nhap du lieu la xoa mem. Danh sach khong hien nut Hoan tac hay banner thanh cong sau xoa; chi thong bao neu co loi. Khoi phuc qua popup Thung rac.
- Nut Xoa vinh vien thay Lam moi: admin chon tung/nhieu/tat ca ket qua, xac nhan so luong/ma (mac dinh No). Xoa Records, ProcessHistories va RecordAttachments trong transaction; giu tep vat ly vi path co the tro toi tep dung chung/tep goc cua nguoi dung. Khong tu dong don thung rac.
- PermanentlyDeleteRecord(code, batchId), LAN records/trash/delete-permanently: chi xoa ho so van nam trong thung rac va dung batch, khong xoa ho so da khoi phuc hay vua bi xoa lai. Ghi SystemLogs, bao ket qua thanh cong/that bai va tai lai popup.

Files:
- `ViewModels/RecordTrashViewModel.cs`, `Views/Records/RecordTrashView.xaml`, `.xaml.cs`
- `ViewModels/RecordListViewModel.cs`, `Views/Records/RecordListView.xaml`
- `Infrastructure/Data/AppDataService.cs`, `Infrastructure/Network/LanDataServer.cs`, `Infrastructure/Network/LanApiModels.cs`, `Models/RecordModels.cs`

Data/API:
- Records them DeletedAt, DeletedBy, DeletionBatchId (TEXT NOT NULL DEFAULT ''). EnsureRecordTrashSchema chay ca sau migration PriorityLevel legacy.
- DeleteRecord(code, batchId) chi danh dau row dang hoat dong; giu Id, trang thai, nguoi xu ly, timestamp nghiep vu, attachment va history.
- RestoreRecord(code, batchId) chi khoi phuc neu batch khop. Danh sach thung rac cu khong the khoi phuc mot lan xoa moi hon.
- GetDeletedRecords / DeleteRecord / RestoreRecord deu kiem tra quyen admin tai service.
- LAN: client moi goi records/trash/move (RecordCode + DeletionBatchId), de khong goi nham xoa cung tren server cu. Server van giu records/delete lam alias xoa mem cho client cu. records/trash tra DeletedRecord[]; records/restore nhan RecordCode + DeletionBatchId, tra bool.
- Truy van nghiep vu dung nguon `(SELECT * FROM Records WHERE DeletedAt = '') AS Records` de loai thung rac khoi danh sach, detail, dashboard, staff, queue, export/count va tim trung. GenerateNextRecordCode va seed van xet toan bo Records de khong tai su dung ma/seed lai khi tat ca bi xoa.
- SaveRecordForm/UpdateProcessingRecord tu choi ho so khong con hoat dong; khong tao moi tu form sua cu.
- Ghi SystemLogs khi chuyen vao thung rac va khoi phuc. Phai cap nhat ca server va WPF client de dung tinh nang LAN.

Verify: build WPF + Server. Da test SQLite rieng, HTTP LAN, migration lap lai, quyen, hoan tac, giu history/tep va cac count/query loai thung rac; render XAML cua so thung rac.
