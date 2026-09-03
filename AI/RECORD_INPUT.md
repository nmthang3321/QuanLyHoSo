# Nhap du lieu

Chi tiet theo chuc nang:
- `AI/pages/RECORD_INPUT_FORM.md`
- `AI/features/AREA_SELECTOR.md`
- `AI/features/ATTACHMENTS.md`

File can mo:
- `ViewModels\RecordInputViewModel.cs`
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `Models\AreaSelectionModels.cs` neu cham toi dia ban.
- `AI/AREA_SELECTOR.md` neu cham toi dia ban.

Service methods:
- `GetNextRecordCode`
- `FindSimilarRecord`
- `SaveRecordForm`
- `DeleteRecord`

Ghi chu:
- App WPF mac dinh chay `Client`. Admin van vao trang Nhap du lieu va luu/sua/xoa ho so qua server API.
- Can bo khong thay muc Nhap du lieu trong sidebar va khong duoc them/xoa ho so.
- Area selector trang nay dung root overlay `AreaOverlayCanvas`, khong dung `Popup`/`ContextMenu`.
- Attachment hien van luu `FilePath` text; chua co upload/copy file tu client len server.
