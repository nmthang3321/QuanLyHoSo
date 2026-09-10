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
- `RecordProcessingViewModel.PrepareQueue()`

Khi bam sidebar Phan loai & Xu ly (`NavigateTo("Processing")` khong co selectedNavigationKey), luon goi `PrepareQueue()` de dong chi tiet/popup va xoa co quay ve trang nguon, sau do reload queue. Mo Phan loai tu Danh sach ho so van giu chi tiet va Back ve Danh sach ho so; neu da bam sidebar thi Back tu chi tiet mo trong queue chi ve queue, sidebar van chon Phan loai & Xu ly.

Moi lan bam truc tiep mot muc tren sidebar, `NavigateTo(..., resetPage: true)` tao lai ViewModel cua trang dich. Trang luon tro ve trang thai ban dau, dong popup/chi tiet va bo bo loc/trang thai tam cua lan mo truoc. Cac luong nghiep vu noi bo nhu Sua ho so, Phan loai tu danh sach va Back khong reset ViewModel.
