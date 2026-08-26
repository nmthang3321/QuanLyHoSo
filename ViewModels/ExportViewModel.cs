using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ExportViewModel : ViewModelBase
    {
        public ExportViewModel()
        {
            Statuses = new ObservableCollection<string> { "Tất cả", "Đang xác minh", "Phân công", "Đã giải quyết", "Chờ kết quả" };
            CaseTypes = new ObservableCollection<string> { "Tất cả", "Khiếu nại", "Tố cáo", "Kiến nghị", "Phản ánh" };
            Fields = new ObservableCollection<string> { "Tất cả", "Quản lý đất đai", "Xây dựng", "Tài nguyên môi trường" };
            Areas = new ObservableCollection<string> { "Tất cả", "Long Xuyên", "Châu Đốc", "Rạch Giá", "Tịnh Biên", "Hà Tiên" };
            Processors = new ObservableCollection<string> { "Tất cả", "Trần Văn B", "Trần Văn C", "Lê Thị D" };
            SortOptions = new ObservableCollection<string> { "Ngày tiếp nhận mới nhất trước", "Ngày tiếp nhận cũ nhất trước", "Trạng thái", "Địa bàn" };

            PreviewRecords = new ObservableCollection<ExportRecordPreview>
            {
                new ExportRecordPreview { Index = 1, RecordCode = "HS-2025-000125", ReceivedDate = "25/08/2026", SenderName = "Nguyễn Văn A", AreaName = "Long Xuyên", CaseType = "Khiếu nại", Field = "Quản lý đất đai", Status = "Đang xác minh" },
                new ExportRecordPreview { Index = 2, RecordCode = "HS-2025-000124", ReceivedDate = "25/08/2026", SenderName = "Trần Văn B", AreaName = "Long Xuyên", CaseType = "Khiếu nại", Field = "Quản lý đất đai", Status = "Phân công" },
                new ExportRecordPreview { Index = 3, RecordCode = "HS-2025-000123", ReceivedDate = "24/08/2026", SenderName = "Lê Văn C", AreaName = "Châu Phú", CaseType = "Tố cáo", Field = "Tài nguyên môi trường", Status = "Đã giải quyết" },
                new ExportRecordPreview { Index = 4, RecordCode = "HS-2025-000122", ReceivedDate = "24/08/2026", SenderName = "Phạm Thị D", AreaName = "Tịnh Biên", CaseType = "Kiến nghị", Field = "Xây dựng", Status = "Chờ kết quả" },
                new ExportRecordPreview { Index = 5, RecordCode = "HS-2025-000121", ReceivedDate = "23/08/2026", SenderName = "Võ Văn E", AreaName = "An Phú", CaseType = "Khiếu nại", Field = "Quản lý đất đai", Status = "Đang xử lý" }
            };
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> Areas { get; }
        public ObservableCollection<string> Processors { get; }
        public ObservableCollection<string> SortOptions { get; }
        public ObservableCollection<ExportRecordPreview> PreviewRecords { get; }
    }
}

