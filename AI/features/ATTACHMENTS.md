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
- Hien chi luu path file goc, chua copy vao managed app storage.
- Khi cap nhat quy trinh den buoc ket qua xu ly ban dau, app hoi co tao 3 file Word tu template `doc\phieu_de_xuat.docx`, `doc\phieu_huong_dan.docx`, `doc\thong_bao.docx` khong. Neu chon Yes, hien progress, tao file, refresh chi tiet va them vao `RecordAttachments`.
- Client LAN chua co upload/attachment API day du.
