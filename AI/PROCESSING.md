# Phan loai & xu ly

Chi tiet theo chuc nang:
- `AI/pages/PROCESSING_QUEUE.md`
- `AI/pages/PROCESSING_DETAIL.md`

File can mo:
- `ViewModels\RecordProcessingViewModel.cs`
- `Views\Records\RecordProcessingView.xaml`

Service methods:
- `GetProcessingQueueMetrics`
- `GetProcessingQueueRecords`
- `CountProcessingQueueRecords`
- `GetProcessingRecordDetail`
- `UpdateProcessingRecord`

Ghi chu:
- Officer chi sua ho so dung ten minh.
- Officer chi duoc cap nhat quy trinh tu buoc `Da phan cong` tro di; khong duoc lui ve `Moi tiep nhan` hoac `Dang phan loai`.
- Admin duoc tuy y cap nhat cac trang thai quy trinh.
- Leader chi xem.
- Area filters trong ViewModel da la `ObservableCollection<AreaSelectionOption>`.
- Khi cap nhat den buoc `Dang cho bo sung tai lieu`/ket qua xu ly ban dau tu step 5 tro di, ViewModel se hoi co tao phieu de xuat/huong dan/thong bao khong. Chon Yes thi bat progress, service moi sinh Word tu `doc\*.docx`, luu attachment va refresh detail.
