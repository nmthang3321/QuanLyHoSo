# Record intake

Detailed references: `engineering/pages/RECORD_INPUT_FORM.md`, `engineering/features/AREA_SELECTOR.md`, and `engineering/features/ATTACHMENTS.md`.

Open `ViewModels\RecordInputViewModel.cs`, `Views\Records\RecordInputView.xaml[.cs]`, `QuanLyHoSo.Shared\RecordCodeRules.cs`, and `Models\AreaSelectionModels.cs` for intake work.

Service methods: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`.

Notes:

- WPF defaults to `Client`; Admin can still access intake and create/edit/delete through the server API.
- Officers do not see the intake menu and cannot create or delete records.
- `Số hồ sơ/Số đơn` is user-editable. The accepted format is `HS-<year>-<6 digits>`, for example `HS-2026-000001`; both client and service boundaries validate it and duplicate codes are rejected.
- The next generated code remains a convenience default and can be replaced by a valid externally supplied code.
- The page uses root overlay `AreaOverlayCanvas`, not `Popup` or `ContextMenu`.
- New attachment drafts include file bytes for LAN upload. The server owns the durable copy under `QuanLyHoSoFiles\Attachments`; client paths are not authoritative after save.
