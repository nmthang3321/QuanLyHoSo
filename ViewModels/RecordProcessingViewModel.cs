using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordProcessingViewModel : ViewModelBase
    {
        public RecordProcessingViewModel()
        {
            Statuses = new ObservableCollection<string>
            {
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Chờ kết quả",
                "Đang chờ bổ sung tài liệu",
                "Đã giải quyết",
                "Chuyển cơ quan khác"
            };

            ProcessSteps = new ObservableCollection<ProcessStep>
            {
                new ProcessStep { StepNumber = 1, IconGlyph = "\uE8A5", Title = "Tiếp nhận", TimeText = "25/08/2026 09:15", IsDone = true },
                new ProcessStep { StepNumber = 2, IconGlyph = "\uE8FD", Title = "Phân loại", TimeText = "25/08/2026 10:20", IsDone = true },
                new ProcessStep { StepNumber = 3, IconGlyph = "\uE77B", Title = "Phân công", TimeText = "25/08/2026 14:05", IsDone = true },
                new ProcessStep { StepNumber = 4, IconGlyph = "\uE721", Title = "Xác minh", TimeText = "25/08/2026 14:30", IsCurrent = true },
                new ProcessStep { StepNumber = 5, IconGlyph = "\uE916", Title = "Gia hạn", TimeText = "Nếu có" },
                new ProcessStep { StepNumber = 6, IconGlyph = "\uE73E", Title = "Kết thúc", TimeText = "Chưa thực hiện" },
                new ProcessStep { StepNumber = 7, IconGlyph = "\uE74E", Title = "Lưu hồ sơ", TimeText = "Chưa thực hiện" }
            };

            History = new ObservableCollection<ProcessHistoryItem>
            {
                new ProcessHistoryItem { Title = "Tiếp nhận", ProcessedAt = "25/08/2026 09:15", ProcessorName = "Trần Văn B", Content = "Tiếp nhận hồ sơ từ công dân trực tiếp tại bộ phận một cửa.", IsCompleted = true },
                new ProcessHistoryItem { Title = "Phân loại", ProcessedAt = "25/08/2026 10:20", ProcessorName = "Trần Văn B", Content = "Phân loại hồ sơ là khiếu nại thuộc lĩnh vực quản lý đất đai.", IsCompleted = true },
                new ProcessHistoryItem { Title = "Phân công", ProcessedAt = "25/08/2026 14:05", ProcessorName = "Trần Văn B", Content = "Phân công cán bộ xác minh Trần Văn C.", IsCompleted = true },
                new ProcessHistoryItem { Title = "Đang xác minh", ProcessedAt = "25/08/2026 14:30", ProcessorName = "Trần Văn C", Content = "Đang tiến hành xác minh nội dung phản ánh.", IsCompleted = false }
            };
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<ProcessStep> ProcessSteps { get; }
        public ObservableCollection<ProcessHistoryItem> History { get; }
    }
}

