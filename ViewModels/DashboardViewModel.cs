using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
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
        private bool _isReloading;
        private double _trendChartWidth = 600;
        private PointCollection _trendLinePoints = new PointCollection();
        private string _totalRecordsText;

        public DashboardViewModel()
        {
            _dataService = AppDataService.Instance;
            var today = DateTime.Today;

            _fromDate = new DateTime(today.Year, today.Month, 1);
            _toDate = today;

            Metrics = new ObservableCollection<DashboardMetric>();
            StatusStats = new ObservableCollection<StatusStat>();
            AreaStats = new ObservableCollection<AreaStat>();
            TrendStats = new ObservableCollection<TrendStat>();
            TrendAxisTicks = new ObservableCollection<TrendAxisTick>();
            RecentRecords = new ObservableCollection<RecentRecord>();
            DateFilterOptions = new ObservableCollection<string>
            {
                ThisWeekFilter,
                ThisMonthFilter,
                ThisYearFilter,
                CustomFilter
            };
            ApplyFilterCommand = new RelayCommand(Reload);
            _previousRecentPageCommand = new RelayCommand(PreviousRecentPage, () => CurrentRecentPage > 1);
            _nextRecentPageCommand = new RelayCommand(NextRecentPage, () => CurrentRecentPage < TotalRecentPages);
            _selectedDateFilter = ThisMonthFilter;

            Reload();
        }

        public ObservableCollection<DashboardMetric> Metrics { get; }
        public ObservableCollection<StatusStat> StatusStats { get; }
        public ObservableCollection<AreaStat> AreaStats { get; }
        public ObservableCollection<TrendStat> TrendStats { get; }
        public ObservableCollection<TrendAxisTick> TrendAxisTicks { get; }
        public ObservableCollection<RecentRecord> RecentRecords { get; }
        public ObservableCollection<string> DateFilterOptions { get; }

        public double TrendChartWidth
        {
            get => _trendChartWidth;
            private set => SetProperty(ref _trendChartWidth, value);
        }

        public PointCollection TrendLinePoints
        {
            get => _trendLinePoints;
            private set => SetProperty(ref _trendLinePoints, value);
        }

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
        public async void Reload()
        {
            if (_isReloading)
            {
                return;
            }

            _isReloading = true;
            var startedAt = DateTime.Now;
            AppLogger.Info("Dashboard", "Reload", "Dashboard reload started.");

            try
            {
                var fromDate = FromDate;
                var toDate = ToDate;
                var previousRange = GetPreviousDateRange();
                var recentPageSize = _recentRecordsPageSize;
                var currentRecentPage = CurrentRecentPage;

                var snapshot = await Task.Run(() =>
                {
                    AppLogger.Info("Dashboard", "Reload", "Loading metrics.");
                    var metrics = _dataService.GetDashboardMetrics(fromDate, toDate, previousRange.FromDate, previousRange.ToDate);
                    AppLogger.Info("Dashboard", "Reload", "Loading status stats.");
                    var statusStats = _dataService.GetStatusStats(fromDate, toDate);
                    AppLogger.Info("Dashboard", "Reload", "Loading area stats.");
                    var areaStats = _dataService.GetTopAreas(fromDate: fromDate, toDate: toDate);
                    AppLogger.Info("Dashboard", "Reload", "Loading trend stats.");
                    var trendStats = _dataService.GetReceivedTrendStats(fromDate, toDate);
                    AppLogger.Info("Dashboard", "Reload", "Counting records.");
                    var totalRecords = _dataService.CountRecords(fromDate, toDate);
                    var totalRecentPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)recentPageSize));
                    var page = Math.Min(currentRecentPage, totalRecentPages);
                    var skip = (page - 1) * recentPageSize;
                    AppLogger.Info("Dashboard", "Reload", "Loading recent records.");
                    var recentRecords = _dataService.GetRecentRecords(recentPageSize, fromDate, toDate, skip);

                    return new DashboardSnapshot
                    {
                        Metrics = metrics,
                        StatusStats = statusStats,
                        AreaStats = areaStats,
                        TrendStats = trendStats,
                        TotalRecords = totalRecords,
                        TotalRecentPages = totalRecentPages,
                        CurrentRecentPage = page,
                        RecentRecords = recentRecords
                    };
                });

                ReplaceItems(Metrics, snapshot.Metrics);
                ReplaceItems(StatusStats, snapshot.StatusStats);
                ReplaceItems(AreaStats, snapshot.AreaStats);
                ReplaceItems(TrendStats, snapshot.TrendStats);
                CurrentRecentPage = snapshot.CurrentRecentPage;
                TotalRecordsText = snapshot.TotalRecords.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
                TotalRecentPages = snapshot.TotalRecentPages;
                ApplyRecentRecordIndexes(snapshot.RecentRecords);
                ReplaceItems(RecentRecords, snapshot.RecentRecords);
                AppLogger.Info("Dashboard", "Reload", "Refreshing trend layout.");
                RefreshTrendChartLayout();

                IsCustomCalendarOpen = false;
                AppLogger.Info("Dashboard", "Reload", $"Dashboard reload completed in {(DateTime.Now - startedAt).TotalMilliseconds:0} ms.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Dashboard", "Reload", ex, "Dashboard reload failed.");
                MessageBox.Show($"Không thể tải trang Tổng quan.\n\nChi tiết: {ex.Message}", "Tổng quan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isReloading = false;
            }
        }

        private (DateTime? FromDate, DateTime? ToDate) GetPreviousDateRange()
        {
            if (!FromDate.HasValue || !ToDate.HasValue)
            {
                return (null, null);
            }

            var currentFrom = FromDate.Value.Date;
            var currentTo = ToDate.Value.Date;

            switch (SelectedDateFilter)
            {
                case ThisYearFilter:
                    return (currentFrom.AddYears(-1), currentTo.AddYears(-1));
                case ThisMonthFilter:
                    var previousMonthFrom = currentFrom.AddMonths(-1);
                    return (previousMonthFrom, previousMonthFrom.AddMonths(1).AddDays(-1));
                case ThisWeekFilter:
                    return (currentFrom.AddDays(-7), currentTo.AddDays(-7));
                default:
                    var dayCount = Math.Max(1, (currentTo - currentFrom).Days + 1);
                    var previousTo = currentFrom.AddDays(-1);
                    var previousFrom = previousTo.AddDays(1 - dayCount);
                    return (previousFrom, previousTo);
            }
        }

        private void RefreshTrendChartLayout()
        {
            const double itemWidth = 78.0;
            TrendChartWidth = Math.Max(600, TrendStats.Count * itemWidth);
            TrendLinePoints = new PointCollection(TrendStats.Select(item =>
                new Point(item.ResolvedPointLeft + 5, item.ResolvedPointY)));
            RefreshTrendAxisTicks();
        }

        private void RefreshTrendAxisTicks()
        {
            const double chartHeight = 150.0;
            var maxValue = TrendStats.Count == 0
                ? 0
                : TrendStats.Max(item => Math.Max(item.ReceivedCount, item.ResolvedCount));
            var tickStep = CalculateNiceAxisStep(maxValue);
            var axisMax = Math.Max(tickStep, (int)Math.Ceiling(maxValue / (double)tickStep) * tickStep);
            var ticks = new List<TrendAxisTick>();

            for (var value = axisMax; value >= 0; value -= tickStep)
            {
                var top = chartHeight * (axisMax - value) / axisMax - 8;
                ticks.Add(new TrendAxisTick
                {
                    Label = value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")),
                    Top = Math.Max(0, Math.Min(chartHeight - 16, top))
                });
            }

            ReplaceItems(TrendAxisTicks, ticks);
        }

        private static int CalculateNiceAxisStep(int value)
        {
            if (value <= 0)
            {
                return 2;
            }

            var targetStep = value / 6.0;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(targetStep)));
            var normalized = targetStep / magnitude;
            double niceNormalized;

            if (normalized <= 1)
            {
                niceNormalized = 1;
            }
            else if (normalized <= 2)
            {
                niceNormalized = 2;
            }
            else if (normalized <= 5)
            {
                niceNormalized = 5;
            }
            else
            {
                niceNormalized = 10;
            }

            return Math.Max(1, (int)Math.Ceiling(niceNormalized * magnitude));
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

            Reload();
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
            ApplyRecentRecordIndexes(records);
            ReplaceItems(RecentRecords, records);
            RaiseRecentPageCommandStates();
        }

        private void ApplyRecentRecordIndexes(IEnumerable<RecentRecord> records)
        {
            var index = (CurrentRecentPage - 1) * _recentRecordsPageSize + 1;
            foreach (var record in records)
            {
                record.Index = index++;
            }
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

        private sealed class DashboardSnapshot
        {
            public IReadOnlyList<DashboardMetric> Metrics { get; set; }
            public IReadOnlyList<StatusStat> StatusStats { get; set; }
            public IReadOnlyList<AreaStat> AreaStats { get; set; }
            public IReadOnlyList<TrendStat> TrendStats { get; set; }
            public int TotalRecords { get; set; }
            public int TotalRecentPages { get; set; }
            public int CurrentRecentPage { get; set; }
            public IReadOnlyList<RecentRecord> RecentRecords { get; set; }
        }
    }
}
