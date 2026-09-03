using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class StaffTrackingViewModel : ViewModelBase
    {
        private const string ThisWeekFilter = "Tuần này";
        private const string ThisMonthFilter = "Tháng này";
        private const string ThisYearFilter = "Năm này";
        private const string CustomFilter = "Khác";
        private const string AllStaffLeadershipScope = "Toàn bộ cán bộ";
        private const string TotalMetricFilter = "Total";
        private const string OverloadedMetricFilter = "Overloaded";
        private const string DueSoonMetricFilter = "DueSoon";
        private const string NeedsAttentionMetricFilter = "NeedsAttention";
        private const int StaffPageSize = 7;
        private const int OverloadedProcessingThreshold = 10;

        private readonly List<StaffPerformanceRow> _allStaffRows = new();
        private readonly List<StaffPerformanceRow> _filteredStaffRows = new();
        private readonly RelayCommand _nextStaffPageCommand;
        private readonly RelayCommand _previousStaffPageCommand;
        private StaffPerformanceRow _selectedStaff;
        private string _selectedLeadershipScope;
        private string _selectedMetricFilter = TotalMetricFilter;
        private int _currentStaffPage = 1;
        private int _totalStaffPages = 1;
        private string _staffRowsSummaryText;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedDateFilter;
        private bool _isCustomCalendarOpen;
        private string _totalDeadlineRecordsText;
        private string _leadershipKpiTargetText = "30";
        private string _leadershipNoticeText;
        private string _leadershipActionStatus;

        public StaffTrackingViewModel()
        {
            AppDataService.Instance.CatalogChanged += DataService_CatalogChanged;

            Metrics = new ObservableCollection<StaffTrackingMetric>();

            StaffRows = new ObservableCollection<StaffPerformanceRow>();
            BarStats = new ObservableCollection<StaffBarStat>();
            DateFilterOptions = new ObservableCollection<string>
            {
                ThisWeekFilter,
                ThisMonthFilter,
                ThisYearFilter,
                CustomFilter
            };
            DeadlineStats = new ObservableCollection<StatusStat>();

            ActiveRecords = new ObservableCollection<StaffWorkRecord>();

            Officers = new ObservableCollection<string> { "Tất cả" };
            LeadershipScopes = new ObservableCollection<string>();

            var today = DateTime.Today;
            FromDate = new DateTime(today.Year, 1, 1);
            ToDate = new DateTime(today.Year, 12, 31);
            _selectedDateFilter = ThisYearFilter;
            SelectedLeadershipScope = AllStaffLeadershipScope;
            LoadLatestLeadershipNotice();

            ApplyFilterCommand = new RelayCommand(RefreshStaffData);
            SaveLeadershipDirectiveCommand = new RelayCommand(SaveLeadershipDirective, () => CanSendLeadershipNotice);
            SelectMetricCommand = new RelayCommand(SelectMetric);
            _previousStaffPageCommand = new RelayCommand(PreviousStaffPage, () => CurrentStaffPage > 1);
            _nextStaffPageCommand = new RelayCommand(NextStaffPage, () => CurrentStaffPage < TotalStaffPages);

            RefreshStaffData();
        }

        public ObservableCollection<StaffTrackingMetric> Metrics { get; }
        public ObservableCollection<StaffPerformanceRow> StaffRows { get; }
        public ObservableCollection<StatusStat> DeadlineStats { get; }
        public ObservableCollection<StaffWorkRecord> ActiveRecords { get; }
        public ObservableCollection<StaffBarStat> BarStats { get; }
        public ObservableCollection<string> DateFilterOptions { get; }
        public ObservableCollection<string> Officers { get; }
        public ObservableCollection<string> LeadershipScopes { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand SaveLeadershipDirectiveCommand { get; }
        public ICommand SelectMetricCommand { get; }
        public ICommand PreviousStaffPageCommand => _previousStaffPageCommand;
        public ICommand NextStaffPageCommand => _nextStaffPageCommand;

        public bool CanSendLeadershipNotice => AuthContext.IsLeader;
        public bool CanReadLeadershipNotice => AuthContext.IsOfficer || AuthContext.IsAdmin;

        public string SelectedOfficer { get; set; }

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

        public string TotalDeadlineRecordsText
        {
            get => _totalDeadlineRecordsText;
            private set => SetProperty(ref _totalDeadlineRecordsText, value);
        }

        public int CurrentStaffPage
        {
            get => _currentStaffPage;
            private set
            {
                if (SetProperty(ref _currentStaffPage, value))
                {
                    OnPropertyChanged(nameof(StaffPageText));
                    RaiseStaffPageCommandStates();
                }
            }
        }

        public int TotalStaffPages
        {
            get => _totalStaffPages;
            private set
            {
                if (SetProperty(ref _totalStaffPages, value))
                {
                    OnPropertyChanged(nameof(StaffPageText));
                    RaiseStaffPageCommandStates();
                }
            }
        }

        public string StaffPageText => $"Trang {CurrentStaffPage}/{TotalStaffPages}";

        public string StaffRowsSummaryText
        {
            get => _staffRowsSummaryText;
            private set => SetProperty(ref _staffRowsSummaryText, value);
        }

        public string SelectedLeadershipScope
        {
            get => _selectedLeadershipScope;
            set => SetProperty(ref _selectedLeadershipScope, value);
        }

        public string LeadershipKpiTargetText
        {
            get => _leadershipKpiTargetText;
            set
            {
                if (SetProperty(ref _leadershipKpiTargetText, value))
                {
                    OnPropertyChanged(nameof(SelectedStaffCompletionText));
                    OnPropertyChanged(nameof(SelectedStaffTargetProgressPercent));
                    OnPropertyChanged(nameof(SelectedStaffTargetProgressText));
                }
            }
        }

        public string LeadershipNoticeText
        {
            get => _leadershipNoticeText;
            set => SetProperty(ref _leadershipNoticeText, value);
        }

        public string LeadershipActionStatus
        {
            get => _leadershipActionStatus;
            set => SetProperty(ref _leadershipActionStatus, value);
        }

        public string SelectedStaffCompletionText => $"{SelectedStaff?.CompletedCount ?? 0} / {GetLeadershipKpiTarget()}";

        public int SelectedStaffTargetProgressPercent
        {
            get
            {
                var target = GetLeadershipKpiTarget();
                if (target <= 0)
                {
                    return 0;
                }

                return Math.Clamp((int)Math.Round((double)(SelectedStaff?.CompletedCount ?? 0) / target * 100d, MidpointRounding.AwayFromZero), 0, 100);
            }
        }

        public string SelectedStaffTargetProgressText => $"{SelectedStaffTargetProgressPercent}%";

        public StaffPerformanceRow SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                if (SetProperty(ref _selectedStaff, value))
                {
                    RefreshLeadershipScopes();
                    RefreshActiveRecords();
                    OnPropertyChanged(nameof(SelectedStaffCompletionText));
                    OnPropertyChanged(nameof(SelectedStaffTargetProgressPercent));
                    OnPropertyChanged(nameof(SelectedStaffTargetProgressText));
                }
            }
        }

        private void RefreshStaffData()
        {
            var staffRows = AppDataService.Instance.GetStaffPerformanceRows(FromDate, ToDate);
            var deadlineStats = AppDataService.Instance.GetStaffDeadlineStats(FromDate, ToDate);
            if (AuthContext.IsOfficer)
            {
                staffRows = staffRows
                    .Where(row => IsCurrentOfficer(row.Name))
                    .ToList();
            }

            if (staffRows.Count == 0)
            {
                staffRows = new List<StaffPerformanceRow>
                {
                    CreateEmptyStaffRow(AuthContext.IsOfficer ? AuthContext.CurrentDisplayName : "Chưa có dữ liệu")
                };
            }

            _allStaffRows.Clear();
            _allStaffRows.AddRange(staffRows);

            RefreshMetricCards(_allStaffRows);
            RefreshDeadlineStats(deadlineStats);
            CurrentStaffPage = 1;
            ApplyMetricFilter();
        }

        private void RefreshDeadlineStats(IEnumerable<StatusStat> deadlineStats)
        {
            DeadlineStats.Clear();
            foreach (var stat in deadlineStats)
            {
                DeadlineStats.Add(stat);
            }

            TotalDeadlineRecordsText = DeadlineStats.Sum(stat => stat.Count).ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        }

        private void ApplyMetricFilter()
        {
            IEnumerable<StaffPerformanceRow> staffRows = _allStaffRows;
            switch (_selectedMetricFilter)
            {
                case OverloadedMetricFilter:
                    staffRows = staffRows.Where(IsOverloadedStaff);
                    break;
                case DueSoonMetricFilter:
                    staffRows = staffRows.Where(row => row.DueSoonCount > 0);
                    break;
                case NeedsAttentionMetricFilter:
                    staffRows = staffRows.Where(NeedsAttention);
                    break;
            }

            _filteredStaffRows.Clear();
            _filteredStaffRows.AddRange(staffRows);
            TotalStaffPages = Math.Max(1, (int)Math.Ceiling(_filteredStaffRows.Count / (double)StaffPageSize));
            if (CurrentStaffPage > TotalStaffPages)
            {
                CurrentStaffPage = TotalStaffPages;
            }

            LoadStaffPage();
        }

        private void LoadStaffPage()
        {
            var skip = (CurrentStaffPage - 1) * StaffPageSize;
            var pageRows = _filteredStaffRows.Skip(skip).Take(StaffPageSize).ToList();

            StaffRows.Clear();
            foreach (var staffRow in pageRows)
            {
                StaffRows.Add(staffRow);
            }

            var fromRow = _filteredStaffRows.Count == 0 ? 0 : skip + 1;
            var toRow = Math.Min(skip + StaffPageSize, _filteredStaffRows.Count);
            StaffRowsSummaryText = $"Hiển thị {fromRow} - {toRow} / {_filteredStaffRows.Count} cán bộ";

            var officerNames = StaffRows.Select(row => row.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Officers.Clear();
            Officers.Add("Tất cả");
            foreach (var officerName in officerNames)
            {
                Officers.Add(officerName);
            }

            BarStats.Clear();
            foreach (var row in StaffRows)
            {
                BarStats.Add(new StaffBarStat
                {
                    StaffName = row.Name,
                    OnTimePercent = int.Parse(row.OnTimeRateText.TrimEnd('%'), CultureInfo.InvariantCulture),
                    KpiPercent = row.KpiPercent,
                    OnTimeHeight = row.KpiPercent > 0 ? int.Parse(row.OnTimeRateText.TrimEnd('%'), CultureInfo.InvariantCulture) : 0,
                    KpiHeight = row.KpiPercent
                });
            }

            SelectedOfficer = Officers.Contains(SelectedOfficer) ? SelectedOfficer : "Tất cả";
            SelectedStaff = StaffRows.FirstOrDefault(row => row.Name == SelectedOfficer) ?? StaffRows.FirstOrDefault();
            RaiseStaffPageCommandStates();
        }

        private void RefreshMetricCards(IReadOnlyCollection<StaffPerformanceRow> staffRows)
        {
            var culture = CultureInfo.GetCultureInfo("vi-VN");
            Metrics.Clear();
            if (AuthContext.IsOfficer)
            {
                var currentStaff = staffRows.FirstOrDefault();
                Metrics.Add(CreateMetric("HỒ SƠ ĐƯỢC GIAO", currentStaff?.AssignedCount ?? 0, "Tổng hồ sơ trong kỳ", "\uE8A5", "#0B5CFF", "#EAF2FF", TotalMetricFilter, culture));
                Metrics.Add(CreateMetric("ĐANG XỬ LÝ", currentStaff?.ProcessingCount ?? 0, "Hồ sơ chưa hoàn thành", "\uE7EF", "#F2A100", "#FFF6DD", OverloadedMetricFilter, culture));
                Metrics.Add(CreateMetric("SẮP TRỄ HẠN", currentStaff?.DueSoonCount ?? 0, "Trong 7 ngày tới", "\uE823", "#FF6B2C", "#FFF0E8", DueSoonMetricFilter, culture));
                Metrics.Add(CreateMetric("QUÁ HẠN", currentStaff?.OverdueCount ?? 0, "Cần xử lý ngay", "\uE7BA", "#E3342F", "#FFECEC", NeedsAttentionMetricFilter, culture));
                return;
            }

            Metrics.Add(CreateMetric("TỔNG CÁN BỘ", staffRows.Count, "Cán bộ có hồ sơ trong kỳ", "\uE716", "#0B5CFF", "#EAF2FF", TotalMetricFilter, culture));
            Metrics.Add(CreateMetric("QUÁ TẢI", staffRows.Count(IsOverloadedStaff), $"Từ {OverloadedProcessingThreshold} hồ sơ đang xử lý", "\uE7EF", "#F2A100", "#FFF6DD", OverloadedMetricFilter, culture));
            Metrics.Add(CreateMetric("SẮP TRỄ HẠN", staffRows.Count(row => row.DueSoonCount > 0), "Có hồ sơ trong 7 ngày tới", "\uE823", "#FF6B2C", "#FFF0E8", DueSoonMetricFilter, culture));
            Metrics.Add(CreateMetric("CẦN ĐÔN ĐỐC", staffRows.Count(NeedsAttention), "Quá hạn hoặc KPI thấp", "\uE7BA", "#E3342F", "#FFECEC", NeedsAttentionMetricFilter, culture));
        }

        private static bool IsOverloadedStaff(StaffPerformanceRow row)
        {
            return row.ProcessingCount >= OverloadedProcessingThreshold;
        }

        private static bool NeedsAttention(StaffPerformanceRow row)
        {
            return row.OverdueCount > 0 || string.Equals(row.KpiStatus, "Cần cải thiện", StringComparison.OrdinalIgnoreCase);
        }

        private StaffTrackingMetric CreateMetric(string title, int value, string note, string iconGlyph, string accentColor, string backgroundColor, string filterKey, CultureInfo culture)
        {
            return new StaffTrackingMetric
            {
                Title = title,
                Value = value.ToString("N0", culture),
                Note = note,
                IconGlyph = iconGlyph,
                AccentColor = accentColor,
                BackgroundColor = backgroundColor,
                FilterKey = filterKey,
                IsSelected = string.Equals(filterKey, _selectedMetricFilter, StringComparison.Ordinal)
            };
        }

        private void SelectMetric(object parameter)
        {
            if (parameter is StaffTrackingMetric metric && !string.IsNullOrWhiteSpace(metric.FilterKey))
            {
                _selectedMetricFilter = metric.FilterKey;
                RefreshMetricCards(_allStaffRows);
                CurrentStaffPage = 1;
                ApplyMetricFilter();
            }
        }

        private void NextStaffPage()
        {
            if (CurrentStaffPage >= TotalStaffPages)
            {
                return;
            }

            CurrentStaffPage++;
            LoadStaffPage();
        }

        private void PreviousStaffPage()
        {
            if (CurrentStaffPage <= 1)
            {
                return;
            }

            CurrentStaffPage--;
            LoadStaffPage();
        }

        private void RaiseStaffPageCommandStates()
        {
            _previousStaffPageCommand?.RaiseCanExecuteChanged();
            _nextStaffPageCommand?.RaiseCanExecuteChanged();
        }

        private void DataService_CatalogChanged(string catalogType)
        {
            if (string.IsNullOrWhiteSpace(catalogType) || string.Equals(catalogType, "ProcessorName", StringComparison.OrdinalIgnoreCase))
            {
                RefreshStaffData();
            }
        }

        private int GetLeadershipKpiTarget()
        {
            return int.TryParse(LeadershipKpiTargetText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var target) && target > 0
                ? target
                : 0;
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

            RefreshStaffData();
        }

        private void SaveLeadershipDirective()
        {
            if (!CanSendLeadershipNotice)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(LeadershipNoticeText))
            {
                LeadershipActionStatus = "Vui lòng nhập nội dung thông báo.";
                return;
            }

            var target = IsSelectedStaffLeadershipScope()
                ? SelectedStaff?.Name ?? "cán bộ đang chọn"
                : "toàn bộ cán bộ";

            AppDataService.Instance.SaveLeadershipNotice(
                IsSelectedStaffLeadershipScope() ? "Staff" : "All",
                IsSelectedStaffLeadershipScope() ? SelectedStaff?.Name : string.Empty,
                LeadershipKpiTargetText,
                LeadershipNoticeText);

            LeadershipActionStatus = $"Đã gửi thông báo cho {target} lúc {DateTime.Now:HH:mm}.";
        }

        private void LoadLatestLeadershipNotice()
        {
            if (!CanReadLeadershipNotice)
            {
                LeadershipNoticeText = string.Empty;
                LeadershipActionStatus = string.Empty;
                return;
            }

            var notice = AppDataService.Instance.GetLatestLeadershipNotice(AuthContext.IsOfficer ? AuthContext.CurrentDisplayName : string.Empty);
            LeadershipNoticeText = notice.Message;
            LeadershipActionStatus = notice.ReceivedText;
        }

        private void RefreshActiveRecords()
        {
            ActiveRecords.Clear();
            var staffName = SelectedStaff?.Name;
            if (string.IsNullOrWhiteSpace(staffName) || string.Equals(staffName, "Chưa có dữ liệu", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var record in AppDataService.Instance.GetStaffActiveRecords(staffName, FromDate, ToDate))
            {
                ActiveRecords.Add(record);
            }
        }

        private static StaffPerformanceRow CreateEmptyStaffRow(string staffName)
        {
            var normalizedName = string.IsNullOrWhiteSpace(staffName) ? "Chưa có dữ liệu" : staffName;
            return new StaffPerformanceRow
            {
                Initials = BuildInitials(normalizedName),
                Name = normalizedName,
                Position = "Chuyên viên",
                AssignedCount = 0,
                ProcessingCount = 0,
                CompletedCount = 0,
                DueSoonCount = 0,
                OverdueCount = 0,
                AverageProcessingTimeText = "0 ngày",
                OnTimeRateText = "0%",
                OnTimeRateColor = "#F97316",
                KpiPercent = 0,
                KpiStatus = "Cần cải thiện",
                KpiStatusBackground = "#FFF0E6",
                KpiStatusForeground = "#EA580C"
            };
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "NV";
            }

            var tokens = name.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return "NV";
            }

            if (tokens.Length == 1)
            {
                var token = tokens[0];
                return token.Substring(0, Math.Min(2, token.Length)).ToUpperInvariant();
            }

            return string.Concat(tokens[0][0], tokens[^1][0]).ToUpperInvariant();
        }

        private static bool IsCurrentOfficer(string staffName)
        {
            return string.Equals(
                (staffName ?? string.Empty).Trim(),
                AuthContext.CurrentDisplayName.Trim(),
                StringComparison.CurrentCultureIgnoreCase);
        }

        private void RefreshLeadershipScopes()
        {
            var useSelectedStaff = IsSelectedStaffLeadershipScope();
            var selectedStaffScope = SelectedStaff?.Name ?? "Chưa chọn cán bộ";

            LeadershipScopes.Clear();
            LeadershipScopes.Add(AllStaffLeadershipScope);
            LeadershipScopes.Add(selectedStaffScope);

            SelectedLeadershipScope = useSelectedStaff ? selectedStaffScope : AllStaffLeadershipScope;
        }

        private bool IsSelectedStaffLeadershipScope()
        {
            return !string.Equals(SelectedLeadershipScope, AllStaffLeadershipScope, StringComparison.OrdinalIgnoreCase);
        }
    }
}
