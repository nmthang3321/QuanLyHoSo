# Feature - Attachments

Use for record attachment and generated-document tasks.

Main files: record input and processing XAML/code-behind/ViewModels, `Models\RecordModels.cs`, `Infrastructure\Data\AppDataService.cs`, `Infrastructure\Network\LanDataClient.cs`, `Infrastructure\Network\LanDataServer.cs`, and `Infrastructure\Documents\InitialResultDocumentGenerator.cs`.

Code-behind entry points include `ChooseAttachmentFilesButton_Click`, `AttachmentDropZone` drag/drop handlers, and `AddAttachmentFiles`.

Database: `RecordAttachments` stores `RecordId`, `FileName`, `FileSize`, and `FilePath`. `FilePath` identifies the server-managed copy after persistence; file bytes are not stored in SQLite.

Rules and behavior:

- Supported formats: PDF, Word `.doc/.docx`, JPG/JPEG, and PNG; maximum 10 MB per file.
- A new `AttachmentDraft` carries the selected file bytes in `Content` across the LAN API. The server validates and writes the file under `QuanLyHoSoFiles\Attachments\<record-code>` beside the server database.
- Generated Word documents are stored under `QuanLyHoSoFiles\GeneratedDocuments` and attached to the record through the same server-owned metadata model.
- Clients open attachments through the authenticated `attachments/download` route. Any local downloaded copy is a temporary cache, not the authoritative file.
- During initialization, accessible legacy attachment paths are migrated into managed server storage. An inaccessible legacy path remains metadata only until the source file is recovered.
- Open/download/delete icon buttons use the shared `AttachmentIconButton` style.
- At workflow step 5, if any generated form is missing, the application can create it from `doc\templates\phieu_de_xuat.docx`, `phieu_huong_dan.docx`, and `thong_bao.docx`. It creates missing forms only and appends them to `RecordAttachments`.
- Generated-form detection uses attachment names, including timestamp suffixes, and does not require a client-local path to exist.
- Full `.qlhbackup` packages include the database, attachments, and generated documents. Legacy `.db` backups include database rows only.
