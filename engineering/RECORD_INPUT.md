# Record intake

Detailed references: `engineering/pages/RECORD_INPUT_FORM.md`, `engineering/features/AREA_SELECTOR.md`, and `engineering/features/ATTACHMENTS.md`.

Open `ViewModels\RecordInputViewModel.cs`, `Views\Records\RecordInputView.xaml[.cs]`, `QuanLyHoSo.Shared\RecordCodeRules.cs`, and `Models\AreaSelectionModels.cs` for intake work.

Service methods: `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`. The legacy `GetNextRecordCode` contract remains for older-client compatibility but returns an empty string.

Notes:

- WPF defaults to `Client`; Admin can still access intake and create/edit/delete through the server API.
- Officers do not see the intake menu and cannot create or delete records.
- `Số hồ sơ/Số đơn` starts blank and is entered entirely by the user. Any non-whitespace value is accepted; surrounding whitespace is trimmed while casing and content are preserved. Duplicate codes are rejected by the live check, save-time validation, service, and database constraint.
- No code is generated or suggested automatically, and the field has no fixed format or length limit.
- The page uses root overlay `AreaOverlayCanvas`, not `Popup` or `ContextMenu`.
- New attachment drafts include file bytes for LAN upload. The server owns the durable copy under `QuanLyHoSoFiles\Attachments`; client paths are not authoritative after save.
