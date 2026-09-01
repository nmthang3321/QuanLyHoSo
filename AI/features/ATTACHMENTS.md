# Feature - Attachments

Dung khi task lien quan file dinh kem ho so.

Files:
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `ViewModels\RecordInputViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`

Code-behind:
- `ChooseAttachmentFilesButton_Click`
- drag/drop handlers tren `AttachmentDropZone`
- `AddAttachmentFiles`

DB:
- Bang `RecordAttachments`
- Columns chinh: `RecordId`, `FileName`, `FileSize`, `FilePath`
- `TryAddColumn` da them/check `FilePath`

Notes:
- Hien chi luu path file goc, chua copy vao managed app storage.
- Client LAN chua co upload/attachment API day du.

