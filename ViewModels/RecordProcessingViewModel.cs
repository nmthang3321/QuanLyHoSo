using System.Collections.ObjectModel;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordProcessingViewModel : ViewModelBase
    {
        public RecordProcessingViewModel()
        {
            var detail = AppDataService.Instance.GetProcessingRecordDetail();

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

            ProcessSteps = new ObservableCollection<ProcessStep>(detail.Steps);
            History = new ObservableCollection<ProcessHistoryItem>(detail.History);

            RecordCode = detail.RecordCode;
            ReceivedDate = detail.ReceivedDate;
            ReceiveSource = detail.ReceiveSource;
            SenderName = detail.SenderName;
            SenderPhone = detail.SenderPhone;
            AreaName = detail.AreaName;
            CaseType = detail.CaseType;
            Field = detail.Field;
            Status = detail.Status;
            ProcessorName = detail.ProcessorName;
            ProcessingDate = detail.ProcessingDate;
            ProcessContent = detail.ProcessContent;
            ProcessNote = detail.ProcessNote;
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<ProcessStep> ProcessSteps { get; }
        public ObservableCollection<ProcessHistoryItem> History { get; }

        public string RecordCode { get; set; }
        public string ReceivedDate { get; set; }
        public string ReceiveSource { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string AreaName { get; set; }
        public string CaseType { get; set; }
        public string Field { get; set; }
        public string Status { get; set; }
        public string ProcessorName { get; set; }
        public string ProcessingDate { get; set; }
        public string ProcessContent { get; set; }
        public string ProcessNote { get; set; }
    }
}
