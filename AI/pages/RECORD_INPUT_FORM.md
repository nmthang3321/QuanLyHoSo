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
- App WPF mac dinh chay `Client`. Admin van vao trang nhap/sua ho so; `FindSimilarRecord`, `SaveRecordForm`, `DeleteRecord` di qua LAN API khi client mode.
- Can bo khong thay muc Nhap du lieu trong sidebar; chi xem/chinh sua/phan loai theo quyen xu ly.
- Dia ban doc them `AI/features/AREA_SELECTOR.md`.
- Attachment doc them `AI/features/ATTACHMENTS.md`.
- Manual save/update `AreaName = $areaName` khong doi khi sua filter dia ban.
