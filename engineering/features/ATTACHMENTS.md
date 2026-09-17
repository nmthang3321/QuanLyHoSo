# Feature - Attachments

Use for record attachment tasks.

Main files: record input and processing XAML/code-behind/ViewModels, `Models\RecordModels.cs`, `Infrastructure\Data\AppDataService.cs`, and `Infrastructure\Documents\InitialResultDocumentGenerator.cs`.

Code-behind entry points include `ChooseAttachmentFilesButton_Click`, `AttachmentDropZone` drag/drop handlers, and `AddAttachmentFiles`.

Database: `RecordAttachments` with `RecordId`, `FileName`, `FileSize`, and `FilePath`. `TryAddColumn` checks/adds `FilePath`.

Rules and limitations:
- Supported formats: PDF, Word `.doc/.docx`, JPG/JPEG, PNG; maximum 10 MB per file.
- Open/download/delete icon buttons use shared `AttachmentIconButton`.
- Only original paths are stored; files are not copied to managed application storage.
- At step 5, if any generated form is missing, the application can create it from `doc\templates\phieu_de_xuat.docx`, `phieu_huong_dan.docx`, and `thong_bao.docx`. It creates missing forms only and appends them to `RecordAttachments`.
- Generated-form detection uses attachment names, including timestamp suffixes, and does not require the client path to exist.
- LAN save/update persists attachment metadata but does not upload physical files. A client-local path may be inaccessible from the server or another workstation.
