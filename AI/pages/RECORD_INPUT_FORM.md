# Page - Nhap du lieu form

Dung khi task lien quan form nhap/sua ho so.

Files:
- `Views\Records\RecordInputView.xaml`
- `Views\Records\RecordInputView.xaml.cs`
- `ViewModels\RecordInputViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetNextRecordCode`
- `FindSimilarRecord`
- `SaveRecordForm`
- `DeleteRecord`

Notes:
- Client mode hien chua cho vao trang nhap/sua ho so de tranh mo SQLite.
- Dia ban doc them `AI/features/AREA_SELECTOR.md`.
- Attachment doc them `AI/features/ATTACHMENTS.md`.
- Manual save/update `AreaName = $areaName` khong doi khi sua filter dia ban.

