using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordListRowViewModel
    {
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
            CanEdit = AuthContext.CanEditRecord(record.ProcessorName);
            CanClassify = AuthContext.CanEditRecord(record.ProcessorName);
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
        public string UpdatedAt { get; }
        public string ProcessorName { get; }
        public bool CanEdit { get; }
        public bool CanClassify { get; }
        public bool CanDelete { get; }
        public ICommand ViewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ClassifyCommand { get; }
        public ICommand DeleteCommand { get; }
    }
}
