using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordTrashRowViewModel : ViewModelBase
    {
        private bool _isSelected;
        public RecordTrashRowViewModel(DeletedRecord record) { Record = record; }
        public DeletedRecord Record { get; }
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public sealed class RecordTrashViewModel : ViewModelBase
    {
        private bool _isBusy;
        private string _message = "Đang tải thùng rác…";
        private string _searchText;
        private readonly RelayCommand _restoreCommand;
        private readonly RelayCommand _deletePermanentlyCommand;
        public RecordTrashViewModel(Action close = null)
        {
            Rows = new ObservableCollection<RecordTrashRowViewModel>();
            FilteredRows = CollectionViewSource.GetDefaultView(Rows);
            FilteredRows.Filter = item =>
            {
                var record = ((RecordTrashRowViewModel)item).Record;
                return string.IsNullOrWhiteSpace(SearchText) ||
                    (record.RecordCode + " " + record.SenderName + " " + record.DeletedBy)
                    .IndexOf(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
            };
            _restoreCommand = new RelayCommand(async () => await RestoreSelectedAsync(), () => !IsBusy && Rows.Any(row => row.IsSelected));
            _deletePermanentlyCommand = new RelayCommand(async () => await DeletePermanentlyAsync(), () => !IsBusy && Rows.Any(row => row.IsSelected));
            CloseCommand = new RelayCommand(() => { if (!IsBusy) close?.Invoke(); }, () => !IsBusy);
        }
        public ObservableCollection<RecordTrashRowViewModel> Rows { get; }
        public ICollectionView FilteredRows { get; }
        public ICommand RestoreCommand => _restoreCommand;
        public ICommand DeletePermanentlyCommand => _deletePermanentlyCommand;
        public ICommand CloseCommand { get; }
        public bool IsBusy { get => _isBusy; private set { SetProperty(ref _isBusy, value); OnPropertyChanged(nameof(IsIdle)); NotifySelection(); } }
        public bool IsIdle => !IsBusy;
        public string Message { get => _message; private set => SetProperty(ref _message, value); }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value)) return;
                foreach (var row in Rows) row.IsSelected = false;
                FilteredRows.Refresh();
                NotifySelection();
            }
        }
        public string SelectionText => $"{FilteredRows.Cast<object>().Count():N0} hồ sơ • Đã chọn {Rows.Count(row => row.IsSelected):N0}";
        public bool? SelectAll
        {
            get
            {
                var visible = FilteredRows.Cast<RecordTrashRowViewModel>().ToList();
                return !visible.Any(row => row.IsSelected) ? false : visible.All(row => row.IsSelected) ? true : (bool?)null;
            }
            set
            {
                if (IsBusy) return;
                foreach (var row in FilteredRows.Cast<RecordTrashRowViewModel>()) row.IsSelected = value == true;
                NotifySelection();
            }
        }
        private void NotifySelection()
        {
            OnPropertyChanged(nameof(SelectAll));
            OnPropertyChanged(nameof(SelectionText));
            _restoreCommand.RaiseCanExecuteChanged();
            _deletePermanentlyCommand.RaiseCanExecuteChanged();
            (CloseCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var records = await Task.Run(() => AppDataService.Instance.GetDeletedRecords());
                Rows.Clear();
                foreach (var record in records)
                {
                    var row = new RecordTrashRowViewModel(record);
                    row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(row.IsSelected)) NotifySelection(); };
                    Rows.Add(row);
                }
                Message = records.Count == 0 ? "Thùng rác trống." : "Khôi phục sẽ giữ nguyên trạng thái, người xử lý, lịch sử và tệp đính kèm.";
            }
            catch (Exception ex)
            {
                Message = "Không thể tải thùng rác. Vui lòng đóng và mở lại thùng rác.";
                AppLogger.Error("Records", "LoadTrash", ex, "Failed to load trash.");
            }
            finally { IsBusy = false; }
        }
        private async Task DeletePermanentlyAsync()
        {
            if (IsBusy) return;
            var records = Rows.Where(row => row.IsSelected).Select(row => row.Record).ToList();
            if (records.Count == 0) return;
            var confirmation = $"Xóa vĩnh viễn {records.Count:N0} hồ sơ đã chọn trong thùng rác?\n\n" +
                string.Join(", ", records.Take(10).Select(record => record.RecordCode)) +
                (records.Count > 10 ? $" … và {records.Count - 10:N0} hồ sơ khác." : "") +
                "\n\nHồ sơ, lịch sử xử lý và thông tin tệp đính kèm sẽ bị xóa khỏi cơ sở dữ liệu. Không thể khôi phục từ thùng rác.";
            if (MessageBox.Show(confirmation, "Xác nhận xóa vĩnh viễn", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            IsBusy = true;
            Message = $"Đang xóa vĩnh viễn {records.Count:N0} hồ sơ…";
            var deleted = 0;
            string failureDetail = null;
            try
            {
                await Task.Run(() =>
                {
                    foreach (var record in records)
                    {
                        try
                        {
                            if (AppDataService.Instance.PermanentlyDeleteRecord(record.RecordCode, record.DeletionBatchId)) deleted++;
                        }
                        catch (Exception ex)
                        {
                            failureDetail ??= ex.Message.Contains("Unknown LAN API route", StringComparison.OrdinalIgnoreCase)
                                ? "Server đang chạy chưa hỗ trợ xóa vĩnh viễn. Hãy cập nhật và khởi động lại QuanLyHoSo.Server bằng bản mới."
                                : ex.Message;
                            AppLogger.Error("Records", "PermanentlyDeleteRecord", ex, "Failed to permanently delete record.", record.RecordCode);
                            if (ex.Message.Contains("Unknown LAN API route", StringComparison.OrdinalIgnoreCase)) break;
                        }
                    }
                });
            }
            finally { IsBusy = false; }
            await LoadAsync();
            var reloadWarning = Message.StartsWith("Không thể tải", StringComparison.Ordinal) ? " " + Message : "";
            Message = $"Đã xóa vĩnh viễn {deleted:N0}/{records.Count:N0} hồ sơ." +
                (deleted < records.Count ? " Một số hồ sơ đã thay đổi hoặc chưa xóa được; hãy kiểm tra thùng rác trước khi thử lại." : "") + reloadWarning;
            if (failureDetail != null)
            {
                Message += " " + failureDetail;
                MessageBox.Show(failureDetail, "Không thể xóa vĩnh viễn", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task RestoreSelectedAsync()
        {
            if (IsBusy) return;
            var records = Rows.Where(row => row.IsSelected).Select(row => row.Record).ToList();
            if (records.Count == 0 || MessageBox.Show($"Khôi phục {records.Count:N0} hồ sơ đã chọn về danh sách làm việc?",
                "Khôi phục hồ sơ", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            IsBusy = true;
            Message = $"Đang khôi phục {records.Count:N0} hồ sơ…";
            var restored = 0;
            try
            {
                await Task.Run(() =>
                {
                    foreach (var record in records)
                    {
                        try { if (AppDataService.Instance.RestoreRecord(record.RecordCode, record.DeletionBatchId)) restored++; }
                        catch (Exception ex) { AppLogger.Error("Records", "RestoreRecord", ex, "Failed to restore record.", record.RecordCode); }
                    }
                });
            }
            finally { IsBusy = false; }
            await LoadAsync();
            var reloadWarning = Message.StartsWith("Không thể tải", StringComparison.Ordinal) ? " " + Message : "";
            Message = $"Đã khôi phục {restored:N0}/{records.Count:N0} hồ sơ. " +
                (restored < records.Count ? "Một số hồ sơ đã thay đổi hoặc chưa khôi phục được; hãy kiểm tra danh sách và thử lại." : "Hồ sơ sẽ hiển thị theo bộ lọc của danh sách làm việc.");
            Message += reloadWarning;
        }
    }
}
