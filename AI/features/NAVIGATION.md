# Feature - Navigation/back/sidebar

Dung khi task lien quan dieu huong, nut Back, sidebar highlight.

Files:
- `ViewModels\ShellViewModel.cs`
- `MainWindow.xaml`
- cac ViewModel trang nguon/dich

Flow can nho:

```text
Dung o Danh sach ho so
-> sidebar highlight Danh sach ho so
-> bam Chi tiet/Phan loai ho so
-> vao trang chi tiet/xu ly nhung sidebar van highlight Danh sach ho so
-> Back
-> ve Danh sach ho so va sidebar van highlight Danh sach ho so
```

Methods:
- `ShellViewModel.NavigateTo(key, selectedNavigationKey)`
- `ShellViewModel.ClassifyRecordFromList(...)`
- `RecordProcessingViewModel.OpenRecord(recordCode, returnToPreviousPage: true)`
- `RecordProcessingViewModel.BackToQueue()`

