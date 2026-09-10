# Page - Danh sach ho so table

Dung khi task lien quan bang ho so, phan trang, thao tac nhanh trong danh sach.

Files:
- `Views\Records\RecordListView.xaml`
- `Views\Records\RecordListView.xaml.cs`
- `ViewModels\RecordListViewModel.cs`
- `ViewModels\RecordListRowViewModel.cs`

Notes:
- DataGrid bind `Records`.
- Bang hien luoi ngang va doc mau nhat de tach hang/cot ro rang.
- Table height bind `TableHeight`.
- Mouse wheel DataGrid forward ve `RecordListScrollViewer`.
- Detail modal doc `AI/popups/RECORD_DETAIL_MODAL.md`.
- Export doc `AI/features/EXPORT_EXCEL.md`.
- Row action `CanEdit` = `AuthContext.CanEditRecord(record.ProcessorName)`. Khong check `AppPathSettings.Current.IsClientMode` tai row nua.
- Bulk selection: mac dinh an cot Chon. Nut Chon ho so luon hien voi admin, bam de bat/tat che do chon nhieu; tat se xoa selection tren moi trang. Khong co menu hay nut Huy chon nhieu rieng. Toolbar co Chon trang nay, Chon tat ca ket qua, Bo chon, Xoa da chon.
- Selection luu theo RecordCode qua cac trang. Chon tat ca lay snapshot toan bo ket qua theo bo loc hien tai (take int.MaxValue), cho phep bo chon tung dong. Doi bo loc/reload/huy che do se xoa selection; chuyen trang giu selection.
- Chon tat ca va xoa nhieu chay background, khoa vung noi dung khi dang xu ly. Xoa xac nhan tong so luong tren moi trang, mac dinh No; goi DeleteRecord hien co cho tung ma (ho tro LAN), bao so thanh cong/that bai, reload cap nhat phan trang.
- Export dung GetExportColumns rieng, khong xuat cot Chon hay trang thai selection.
- Tham khao UI: https://www.patternfly.org/patterns/bulk-selection/
- Xoa hien la xoa mem; khong co nut Hoan tac/banner thanh cong. Khoi phuc qua nut Thung rac canh Chon ho so. Doc `AI/features/RECORD_TRASH.md`.
