using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        public DashboardViewModel()
        {
            Metrics = new ObservableCollection<DashboardMetric>
            {
                new DashboardMetric { Title = "TỔNG HỒ SƠ", Value = "1.248", Delta = "+32 hồ sơ so với kỳ trước", IconGlyph = "\uE8A5", AccentColor = "#0B5CFF" },
                new DashboardMetric { Title = "ĐANG XỬ LÝ", Value = "125", Delta = "+8 hồ sơ so với kỳ trước", IconGlyph = "\uE823", AccentColor = "#F28C18" },
                new DashboardMetric { Title = "ĐÃ GIẢI QUYẾT", Value = "980", Delta = "+45 hồ sơ so với kỳ trước", IconGlyph = "\uE73E", AccentColor = "#1FA24A" },
                new DashboardMetric { Title = "CHỜ KẾT QUẢ", Value = "43", Delta = "-5 hồ sơ so với kỳ trước", IconGlyph = "\uE916", AccentColor = "#7147D8" }
            };

            StatusStats = new ObservableCollection<StatusStat>
            {
                new StatusStat { Name = "Đã giải quyết", Count = 850, Percentage = "68.1%", Color = "#24A148" },
                new StatusStat { Name = "Đang xác minh", Count = 210, Percentage = "16.8%", Color = "#F5B132" },
                new StatusStat { Name = "Đang phân loại", Count = 70, Percentage = "5.6%", Color = "#2F73FF" },
                new StatusStat { Name = "Chờ kết quả", Count = 55, Percentage = "4.5%", Color = "#7B4DE3" },
                new StatusStat { Name = "Chờ bổ sung tài liệu", Count = 38, Percentage = "3.0%", Color = "#FF5A1F" },
                new StatusStat { Name = "Chuyển cơ quan khác", Count = 25, Percentage = "2.0%", Color = "#1F4AB8" }
            };

            AreaStats = new ObservableCollection<AreaStat>
            {
                new AreaStat { AreaName = "Phường Long Xuyên", Count = 245, Width = 270 },
                new AreaStat { AreaName = "Phường Châu Đốc", Count = 198, Width = 218 },
                new AreaStat { AreaName = "Phường Rạch Giá", Count = 165, Width = 182 },
                new AreaStat { AreaName = "Phường Tịnh Biên", Count = 132, Width = 145 },
                new AreaStat { AreaName = "Phường Hà Tiên", Count = 118, Width = 130 }
            };

            RecentRecords = new ObservableCollection<RecentRecord>
            {
                new RecentRecord { RecordCode = "HS-00125", SenderName = "Nguyễn Văn A", AreaName = "Phường Long Xuyên", Status = "Đang xác minh", UpdatedAt = "25/08/2026 09:15", ProcessorName = "Trần Văn B" },
                new RecentRecord { RecordCode = "HS-00124", SenderName = "Trần Văn B", AreaName = "Phường Châu Đốc", Status = "Chờ kết quả", UpdatedAt = "25/08/2026 08:40", ProcessorName = "Trần Văn B" },
                new RecentRecord { RecordCode = "HS-00123", SenderName = "Lê Văn C", AreaName = "Phường Rạch Giá", Status = "Đã giải quyết", UpdatedAt = "24/08/2026 16:20", ProcessorName = "Trần Văn B" },
                new RecentRecord { RecordCode = "HS-00122", SenderName = "Phạm Thị D", AreaName = "Phường Tịnh Biên", Status = "Đang phân loại", UpdatedAt = "24/08/2026 14:05", ProcessorName = "Trần Văn B" }
            };
        }

        public ObservableCollection<DashboardMetric> Metrics { get; }
        public ObservableCollection<StatusStat> StatusStats { get; }
        public ObservableCollection<AreaStat> AreaStats { get; }
        public ObservableCollection<RecentRecord> RecentRecords { get; }
    }
}

