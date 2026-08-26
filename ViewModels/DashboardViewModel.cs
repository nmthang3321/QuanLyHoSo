using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private const string ThisWeekFilter = "Tuần này";
        private const string ThisMonthFilter = "Tháng này";
        private const string ThisYearFilter = "Năm này";
        private const string CustomFilter = "Khác";

        private const int DefaultRecentRecordsPageSize = 5;
        private const int MinimumRecentRecordsPageSize = 1;
        private const int MaximumRecentRecordsPageSize = 20;
        private readonly AppDataService _dataService;
        private readonly RelayCommand _nextRecentPageCommand;
        private readonly RelayCommand _previousRecentPageCommand;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private bool _isCustomCalendarOpen;
        private int _currentRecentPage = 1;
        private int _recentRecordsPageSize = DefaultRecentRecordsPageSize;
        private int _totalRecentPages = 1;
        private string _recentRecordsPageSizeText = DefaultRecentRecordsPageSize.ToString(CultureInfo.InvariantCulture);
        private string _selectedDateFilter;
        private string _totalRecordsText;

        public DashboardViewModel()
        {
            _dataService = AppDataService.Instance;
            var today = DateTime.Today;

            _fromDate = new DateTime(today.Year, today.Month, 1);
            _toDate = _fromDate.Value.AddMonths(1).AddDays(-1);

            Metrics = new ObservableCollection<DashboardMetric>();
            StatusStats = new ObservableCollection<StatusStat>();
            AreaStats = new ObservableCollection<AreaStat>();
            RecentRecords = new ObservableCollection<RecentRecord>();
            DateFilterOptions = new ObservableCollection<string>
            {
                ThisWeekFilter,
                ThisMonthFilter,
                ThisYearFilter,
                CustomFilter
            };
            ApplyFilterCommand = new RelayCommand(ReloadFromFirstRecentPage);
            _previousRecentPageCommand = new RelayCommand(PreviousRecentPage, () => CurrentRecentPage > 1);
            _nextRecentPageCommand = new RelayCommand(NextRecentPage, () => CurrentRecentPage < TotalRecentPages);
            _selectedDateFilter = ThisMonthFilter;

            Reload();
        }

        public ObservableCollection<DashboardMetric> Metrics { get; }
        public ObservableCollection<StatusStat> StatusStats { get; }
        public ObservableCollection<AreaStat> AreaStats { get; }
        public ObservableCollection<RecentRecord> RecentRecords { get; }
        public ObservableCollection<string> DateFilterOptions { get; }

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    OnPropertyChanged(nameof(DateRangeText));
                }
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    OnPropertyChanged(nameof(DateRangeText));
                }
            }
        }

        public string SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (!SetProperty(ref _selectedDateFilter, value))
                {
                    return;
                }

                ApplyPresetDateRange(value);
                OnPropertyChanged(nameof(DateRangeText));
            }
        }

        public string DateRangeText
        {
            get
            {
                var fromText = FromDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "--/--/----";
                var toText = ToDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "--/--/----";
                return $"{SelectedDateFilter} ({fromText} - {toText})";
            }
        }

        public bool IsCustomCalendarOpen
        {
            get => _isCustomCalendarOpen;
            set => SetProperty(ref _isCustomCalendarOpen, value);
        }

        public string TotalRecordsText
        {
            get => _totalRecordsText;
            private set => SetProperty(ref _totalRecordsText, value);
        }

        public ICommand ApplyFilterCommand { get; }
        public ICommand PreviousRecentPageCommand => _previousRecentPageCommand;
        public ICommand NextRecentPageCommand => _nextRecentPageCommand;

        public int CurrentRecentPage
        {
            get => _currentRecentPage;
            private set
            {
                if (SetProperty(ref _currentRecentPage, value))
                {
                    OnPropertyChanged(nameof(RecentPageText));
                    RaiseRecentPageCommandStates();
                }
            }
        }

        public int TotalRecentPages
        {
            get => _totalRecentPages;
            private set
            {
                if (SetProperty(ref _totalRecentPages, value))
                {
                    OnPropertyChanged(nameof(RecentPageText));
                    RaiseRecentPageCommandStates();
                }
            }
        }

        public string RecentPageText => $"Trang {CurrentRecentPage}/{TotalRecentPages}";

        public string RecentRecordsPageSizeText
        {
            get => _recentRecordsPageSizeText;
            set
            {
                if (!SetProperty(ref _recentRecordsPageSizeText, value))
                {
                    return;
                }

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageSize))
                {
                    return;
                }

                pageSize = Math.Max(MinimumRecentRecordsPageSize, Math.Min(MaximumRecentRecordsPageSize, pageSize));
                var normalizedPageSizeText = pageSize.ToString(CultureInfo.InvariantCulture);
                if (!string.Equals(_recentRecordsPageSizeText, normalizedPageSizeText, StringComparison.Ordinal))
                {
                    _recentRecordsPageSizeText = normalizedPageSizeText;
                    OnPropertyChanged(nameof(RecentRecordsPageSizeText));
                }

                if (_recentRecordsPageSize == pageSize)
                {
                    return;
                }

                _recentRecordsPageSize = pageSize;
                OnPropertyChanged(nameof(RecentTableHeight));
                ReloadFromFirstRecentPage();
            }
        }

        public int RecentTableHeight => 38 + _recentRecordsPageSize * 34;
        private void Reload()
        {
            ReplaceItems(Metrics, _dataService.GetDashboardMetrics(FromDate, ToDate));
            ReplaceItems(StatusStats, _dataService.GetStatusStats(FromDate, ToDate));
            ReplaceItems(AreaStats, _dataService.GetTopAreas(fromDate: FromDate, toDate: ToDate));
            var totalRecords = _dataService.CountRecords(FromDate, ToDate);
            TotalRecordsText = totalRecords.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
            TotalRecentPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)_recentRecordsPageSize));
            if (CurrentRecentPage > TotalRecentPages)
            {
                CurrentRecentPage = TotalRecentPages;
            }

            LoadRecentRecordsPage();
            IsCustomCalendarOpen = false;
        }

        private void ApplyPresetDateRange(string filter)
        {
            var today = DateTime.Today;

            switch (filter)
            {
                case ThisWeekFilter:
                    var daysSinceMonday = today.DayOfWeek == DayOfWeek.Sunday
                        ? 6
                        : (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                    FromDate = today.AddDays(-daysSinceMonday);
                    ToDate = FromDate.Value.AddDays(6);
                    break;
                case ThisYearFilter:
                    FromDate = new DateTime(today.Year, 1, 1);
                    ToDate = new DateTime(today.Year, 12, 31);
                    break;
                case CustomFilter:
                    IsCustomCalendarOpen = true;
                    return;
                default:
                    FromDate = new DateTime(today.Year, today.Month, 1);
                    ToDate = FromDate.Value.AddMonths(1).AddDays(-1);
                    break;
            }

            ReloadFromFirstRecentPage();
        }

        private void ReloadFromFirstRecentPage()
        {
            CurrentRecentPage = 1;
            Reload();
        }

        private void NextRecentPage()
        {
            if (CurrentRecentPage >= TotalRecentPages)
            {
                return;
            }

            CurrentRecentPage++;
            LoadRecentRecordsPage();
        }

        private void PreviousRecentPage()
        {
            if (CurrentRecentPage <= 1)
            {
                return;
            }

            CurrentRecentPage--;
            LoadRecentRecordsPage();
        }

        private void LoadRecentRecordsPage()
        {
            var skip = (CurrentRecentPage - 1) * _recentRecordsPageSize;
            var records = _dataService.GetRecentRecords(_recentRecordsPageSize, FromDate, ToDate, skip);
            var index = skip + 1;
            foreach (var record in records)
            {
                record.Index = index++;
            }

            ReplaceItems(RecentRecords, records);
            RaiseRecentPageCommandStates();
        }

        private void RaiseRecentPageCommandStates()
        {
            _previousRecentPageCommand.RaiseCanExecuteChanged();
            _nextRecentPageCommand.RaiseCanExecuteChanged();
        }

        private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}
