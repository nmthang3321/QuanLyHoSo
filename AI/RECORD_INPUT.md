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
- Client mode hien chua cho vao trang nhap/sua ho so de tranh mo SQLite; can noi API rieng neu user yeu cau.
- Area selector trang nay dung root overlay `AreaOverlayCanvas`, khong dung `Popup`/`ContextMenu`.
