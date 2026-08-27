using System.Windows.Input;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordListRowViewModel
    {
        public RecordListRowViewModel(RecentRecord record, ICommand viewCommand, ICommand editCommand, ICommand deleteCommand)
        {
            Index = record.Index;
            RecordCode = record.RecordCode;
            SenderName = record.SenderName;
            AreaName = record.AreaName;
            Status = record.Status;
            UpdatedAt = record.UpdatedAt;
            ProcessorName = record.ProcessorName;
            ViewCommand = viewCommand;
            EditCommand = editCommand;
            DeleteCommand = deleteCommand;
        }

        public int Index { get; }
        public string RecordCode { get; }
        public string SenderName { get; }
        public string AreaName { get; }
        public string Status { get; }
        public string UpdatedAt { get; }
        public string ProcessorName { get; }
        public ICommand ViewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
    }
}
