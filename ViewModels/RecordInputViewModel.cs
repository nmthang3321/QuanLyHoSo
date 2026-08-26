using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordInputViewModel : ViewModelBase
    {
        public RecordInputViewModel()
        {
            ReceiveSources = new ObservableCollection<string> { "Trực tiếp", "Qua bưu điện", "Cổng thông tin", "Cơ quan chuyển đến" };
            Areas = new ObservableCollection<string> { "Phường Long Xuyên", "Phường Châu Đốc", "Phường Rạch Giá", "Phường Tịnh Biên", "Phường Hà Tiên" };
            CaseTypes = new ObservableCollection<string> { "Khiếu nại", "Tố cáo", "Kiến nghị", "Phản ánh" };
            Fields = new ObservableCollection<string> { "Quản lý đất đai", "Xây dựng", "Tài nguyên môi trường", "Trật tự đô thị" };
            ContentGroups = new ObservableCollection<string> { "Đất đai - Xây dựng", "Môi trường", "An ninh trật tự", "Khác" };
            Priorities = new ObservableCollection<string> { "Bình thường", "Ưu tiên", "Khẩn" };
            Attachments = new ObservableCollection<AttachmentDraft>
            {
                new AttachmentDraft { FileName = "Đơn khiếu nại (Nguyễn Văn A).pdf", FileSize = "512 KB" },
                new AttachmentDraft { FileName = "Hình ảnh hiện trạng.pdf", FileSize = "1.2 MB" },
                new AttachmentDraft { FileName = "Giấy chứng nhận quyền sử dụng đất.pdf", FileSize = "842 KB" }
            };
        }

        public ObservableCollection<string> ReceiveSources { get; }
        public ObservableCollection<string> Areas { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> ContentGroups { get; }
        public ObservableCollection<string> Priorities { get; }
        public ObservableCollection<AttachmentDraft> Attachments { get; }
    }
}

