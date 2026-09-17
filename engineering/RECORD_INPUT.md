# Record intake

Detailed references: `engineering/pages/RECORD_INPUT_FORM.md`, `engineering/features/AREA_SELECTOR.md`, and `engineering/features/ATTACHMENTS.md`.

Open `ViewModels\RecordInputViewModel.cs`, `Views\Records\RecordInputView.xaml[.cs]`, and `Models\AreaSelectionModels.cs` for area work.

Service methods: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, and `DeleteRecord`.

Notes:
- WPF defaults to `Client`; Admin can still access intake and create/edit/delete through the server API.
- Officers do not see the intake menu and cannot create or delete records.
- The page uses root overlay `AreaOverlayCanvas`, not `Popup` or `ContextMenu`.
- Attachments currently persist `FilePath` text only; physical client-to-server upload/copy is not implemented.
