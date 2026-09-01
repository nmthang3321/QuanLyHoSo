# Page - Phan loai & xu ly detail/update

Dung khi task lien quan mo chi tiet xu ly, cap nhat trang thai/lich su.

Files:
- `Views\Records\RecordProcessingView.xaml`
- `ViewModels\RecordProcessingViewModel.cs`
- `Models\RecordModels.cs`
- `Infrastructure\Data\AppDataService.cs`

Service methods:
- `GetProcessingRecordDetail`
- `UpdateProcessingRecord`

Notes:
- `ProcessingRecordDetail` nam trong `Models\RecordModels.cs`.
- Navigation/back/sidebar highlight lien quan `ShellViewModel`.
- Role: Officer chi sua ho so minh phu trach, Leader chi xem.

