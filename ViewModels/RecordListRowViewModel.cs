using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordListRowViewModel : ViewModelBase
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, CanDelete && value);
        }

        public RecordListRowViewModel(RecentRecord record, ICommand viewCommand, ICommand editCommand, ICommand classifyCommand, ICommand deleteCommand)
        {
            Index = record.Index;
            RecordCode = record.RecordCode;
            SenderName = record.SenderName;
            AreaName = record.AreaName;
            CaseType = record.CaseType;
            Field = record.Field;
            ReceivedDate = record.ReceivedDate;
            Status = record.Status;
            UpdatedAt = record.UpdatedAt;
            ProcessorName = record.ProcessorName;
            CommanderApproverName = record.CommanderApproverName ?? string.Empty;
            LeaderApproverName = record.LeaderApproverName ?? string.Empty;
            TransferDocumentNumber = record.TransferDocumentNumber ?? string.Empty;
            TransferDocumentDate = record.TransferDocumentDate ?? string.Empty;
            TransferredToAgency = record.TransferredToAgency ?? string.Empty;
            AgencyResult = record.AgencyResult ?? string.Empty;
            OriginalRecordCode = record.OriginalRecordCode ?? string.Empty;
            CanEdit = AuthContext.CanEditRecord(record.ProcessorName);
            CanClassify = AuthContext.IsLeader || AuthContext.CanEditRecord(record.ProcessorName);
            ProcessingActionToolTip = IsResubmission
                ? (AuthContext.IsLeader ? "Xem chi tiết xử lý hồ sơ gốc (chỉ xem)" : "Phân loại / xử lý trên hồ sơ gốc")
                : (AuthContext.IsLeader ? "Xem chi tiết xử lý (chỉ xem)" : "Phân loại / xử lý");
            CanDelete = AuthContext.CanDeleteRecord;
            ViewCommand = viewCommand;
            EditCommand = editCommand;
            ClassifyCommand = classifyCommand;
            DeleteCommand = deleteCommand;
        }

        public int Index { get; }
        public string RecordCode { get; }
        public string SenderName { get; }
        public string AreaName { get; }
        public string CaseType { get; }
        public string Field { get; }
        public string ReceivedDate { get; }
        public string Status { get; }
        public string StatusDisplay => Status == RecordStatuses.ResubmittedResolved ? "Hồ sơ gửi lại" : Status;
        public string UpdatedAt { get; }
        public string ProcessorName { get; }
        public string CommanderApproverName { get; }
        public string LeaderApproverName { get; }
        public string TransferDocumentNumber { get; }
        public string TransferDocumentDate { get; }
        public string TransferredToAgency { get; }
        public string AgencyResult { get; }
        public string OriginalRecordCode { get; }
        public bool IsResubmission => !string.IsNullOrWhiteSpace(OriginalRecordCode);
        public bool CanEdit { get; }
        public bool CanClassify { get; }
        public string ProcessingActionToolTip { get; }
        public bool CanDelete { get; }
        public ICommand ViewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ClassifyCommand { get; }
        public ICommand DeleteCommand { get; }
    }
}
