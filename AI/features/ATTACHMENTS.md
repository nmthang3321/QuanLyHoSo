# Feature - Attachments

Dung khi task lien quan file dinh kem ho so.

Files:
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `ViewModels\RecordInputViewModel.cs`
- `Views\Records\RecordProcessingView.xaml`
- `Views\Records\RecordProcessingView.xaml.cs`
- `ViewModels\RecordProcessingViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`
- `Infrastructure\Documents\InitialResultDocumentGenerator.cs`

Code-behind:
- `ChooseAttachmentFilesButton_Click`
- drag/drop handlers tren `AttachmentDropZone`
- `AddAttachmentFiles`

DB:
- Bang `RecordAttachments`
- Columns chinh: `RecordId`, `FileName`, `FileSize`, `FilePath`
- `TryAddColumn` da them/check `FilePath`

Notes:
- Ho tro dinh kem: PDF, Word `.doc/.docx`, JPG/JPEG, PNG; toi da 10MB/file.
- Nut icon mo/tai/xoa dung `AttachmentIconButton` trong `App.xaml`: icon phang khong vien, mau ro va hover nen xanh nhat nhu nut thao tac trong danh sach ho so.
- Hien chi luu path file goc, chua copy vao managed app storage.
- Chi khi cap nhat o buoc 5 (ket qua xu ly ban dau) va chua du phieu, app moi hoi tao file Word tu template `doc\phieu_de_xuat.docx`, `doc\phieu_huong_dan.docx`, `doc\thong_bao.docx`. Neu chon Yes, chi tao file con thieu, refresh chi tiet va them vao `RecordAttachments`, khong hien progress. Nhan dien file theo ten attachment (ke ca hau to timestamp do generator tao), khong kiem tra path tren may client vi file co the nam tren server.
- Client LAN da luu duoc metadata attachment khi save/update ho so qua API, nhung chua co upload/copy file vat ly tu client len server. Neu path file nam tren may client, may khac/server co the khong mo duoc file do.
