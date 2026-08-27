using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordListViewModel : ViewModelBase
    {
        private const int DefaultPageSize = 20;
        private const int MinimumPageSize = 1;
        private const int MaximumPageSize = 20;

        private readonly AppDataService _dataService;
        private readonly Action _goBack;
        private readonly Action<string> _editRecord;
        private readonly RelayCommand _nextPageCommand;
        private readonly RelayCommand _previousPageCommand;
        private readonly RelayCommand _refreshCommand;
        private int _currentPage = 1;
        private int _pageSize = DefaultPageSize;
        private string _pageSizeText = DefaultPageSize.ToString(CultureInfo.InvariantCulture);
        private RecordFormDraft _selectedRecordDetail;
        private int _totalPages = 1;
        private string _totalRecordsText;

        public RecordListViewModel(Action goBack, Action<string> editRecord)
        {
            _dataService = AppDataService.Instance;
            _goBack = goBack ?? (() => { });
            _editRecord = editRecord ?? (_ => { });
            Records = new ObservableCollection<RecordListRowViewModel>();

            _previousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            _nextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            _refreshCommand = new RelayCommand(ReloadFromFirstPage);
            BackCommand = new RelayCommand(_goBack);
            CloseDetailCommand = new RelayCommand(CloseDetail);

            Reload();
        }

        public ObservableCollection<RecordListRowViewModel> Records { get; }
        public ICommand PreviousPageCommand => _previousPageCommand;
        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand RefreshCommand => _refreshCommand;
        public ICommand BackCommand { get; }
        public ICommand CloseDetailCommand { get; }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(PageText));
                    RaisePageCommandStates();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    OnPropertyChanged(nameof(PageText));
                    RaisePageCommandStates();
                }
            }
        }

        public string PageText => $"Trang {CurrentPage}/{TotalPages}";

        public string PageSizeText
        {
            get => _pageSizeText;
            set
            {
                if (!SetProperty(ref _pageSizeText, value))
                {
                    return;
                }

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageSize))
                {
                    return;
                }

                pageSize = Math.Max(MinimumPageSize, Math.Min(MaximumPageSize, pageSize));
                var normalizedPageSizeText = pageSize.ToString(CultureInfo.InvariantCulture);
                if (!string.Equals(_pageSizeText, normalizedPageSizeText, StringComparison.Ordinal))
                {
                    _pageSizeText = normalizedPageSizeText;
                    OnPropertyChanged(nameof(PageSizeText));
                }

                if (_pageSize == pageSize)
                {
                    return;
                }

                _pageSize = pageSize;
                OnPropertyChanged(nameof(TableHeight));
                ReloadFromFirstPage();
            }
        }

        public string TotalRecordsText
        {
            get => _totalRecordsText;
            private set => SetProperty(ref _totalRecordsText, value);
        }

        public int TableHeight => 38 + _pageSize * 34;

        public RecordFormDraft SelectedRecordDetail
        {
            get => _selectedRecordDetail;
            private set
            {
                if (SetProperty(ref _selectedRecordDetail, value))
                {
                    OnPropertyChanged(nameof(IsDetailOpen));
                }
            }
        }

        public bool IsDetailOpen => SelectedRecordDetail != null;

        public void Reload()
        {
            var totalRecords = _dataService.CountRecords();
            TotalRecordsText = $"{totalRecords:N0} hồ sơ";
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)_pageSize));
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            LoadPage();
        }

        private void ReloadFromFirstPage()
        {
            CurrentPage = 1;
            Reload();
        }

        private void NextPage()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }

            CurrentPage++;
            LoadPage();
        }

        private void PreviousPage()
        {
            if (CurrentPage <= 1)
            {
                return;
            }

            CurrentPage--;
            LoadPage();
        }

        private void LoadPage()
        {
            var skip = (CurrentPage - 1) * _pageSize;
            var records = _dataService.GetRecentRecords(_pageSize, skip: skip);
            var rows = new List<RecordListRowViewModel>();
            var index = skip + 1;
            foreach (var record in records)
            {
                record.Index = index++;
                rows.Add(new RecordListRowViewModel(
                    record,
                    new RelayCommand(() => ViewRecord(record.RecordCode)),
                    new RelayCommand(() => EditRecord(record.RecordCode)),
                    new RelayCommand(() => DeleteRecord(record.RecordCode))));
            }

            Records.Clear();
            foreach (var row in rows)
            {
                Records.Add(row);
            }

            RaisePageCommandStates();
        }

        private void ViewRecord(string recordCode)
        {
            SelectedRecordDetail = _dataService.GetRecordForm(recordCode);
        }

        private void CloseDetail()
        {
            SelectedRecordDetail = null;
        }

        private void EditRecord(string recordCode)
        {
            _editRecord(recordCode);
        }

        private void DeleteRecord(string recordCode)
        {
            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa hồ sơ {recordCode}?",
                "Xác nhận xóa hồ sơ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (_dataService.DeleteRecord(recordCode))
            {
                MessageBox.Show("Đã xóa hồ sơ khỏi cơ sở dữ liệu.", "Xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
                Reload();
            }
        }

        private void RaisePageCommandStates()
        {
            _previousPageCommand.RaiseCanExecuteChanged();
            _nextPageCommand.RaiseCanExecuteChanged();
        }
    }
}
