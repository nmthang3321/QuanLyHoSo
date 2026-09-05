using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
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
        private const int StaffPageSize = 5;
        private const int NotificationPageSize = 5;
        private const int OverloadedProcessingThreshold = 10;

        private readonly Action<int> _notificationUnreadCountChanged;
        private readonly List<StaffPerformanceRow> _allStaffRows = new();
        private readonly List<StaffPerformanceRow> _filteredStaffRows = new();
        private readonly RelayCommand _nextStaffPageCommand;
        private readonly RelayCommand _previousStaffPageCommand;
        private readonly RelayCommand _nextNotificationPageCommand;
        private readonly RelayCommand _previousNotificationPageCommand;
        private readonly RelayCommand _markNotificationsReadCommand;
        private readonly RelayCommand _openNotificationCommand;
        private readonly RelayCommand _closeNotificationCommand;
        private StaffPerformanceRow _selectedStaff;
        private StaffNotification _selectedNotification;
        private string _selectedLeadershipScope;
        private string _selectedMetricFilter = TotalMetricFilter;
        private int _currentStaffPage = 1;
        private int _totalStaffPages = 1;
        private int _currentNotificationPage = 1;
        private int _totalNotificationPages = 1;
        private int _unreadNotificationCount;
        private string _staffRowsSummaryText;
        private string _notificationSummaryText;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedDateFilter;
        private bool _isCustomCalendarOpen;
        private bool _isNotificationDetailOpen;
        private string _totalDeadlineRecordsText;
        private string _leadershipKpiTargetText = "30";
        private string _leadershipNoticeText;
        private string _leadershipActionStatus;
        private string _leadershipKpiStatus;

        public StaffTrackingViewModel(Action<int> notificationUnreadCountChanged = null)
        {
            _notificationUnreadCountChanged = notificationUnreadCountChanged;
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
            Notifications = new ObservableCollection<StaffNotification>();

            Officers = new ObservableCollection<string> { "Tất cả" };
            LeadershipScopes = new ObservableCollection<string>();

            var today = DateTime.Today;
            FromDate = new DateTime(today.Year, 1, 1);
            ToDate = new DateTime(today.Year, 12, 31);
            _selectedDateFilter = ThisYearFilter;
            SelectedLeadershipScope = AllStaffLeadershipScope;

            ApplyFilterCommand = new RelayCommand(RefreshStaffData);
            SaveLeadershipDirectiveCommand = new RelayCommand(SaveLeadershipDirective, () => CanSendLeadershipNotice);
            SaveLeadershipKpiCommand = new RelayCommand(SaveLeadershipKpi, () => CanSetLeadershipKpi);
            SelectMetricCommand = new RelayCommand(SelectMetric);
            _previousStaffPageCommand = new RelayCommand(PreviousStaffPage, () => CurrentStaffPage > 1);
            _nextStaffPageCommand = new RelayCommand(NextStaffPage, () => CurrentStaffPage < TotalStaffPages);
            _previousNotificationPageCommand = new RelayCommand(PreviousNotificationPage, () => CurrentNotificationPage > 1);
            _nextNotificationPageCommand = new RelayCommand(NextNotificationPage, () => CurrentNotificationPage < TotalNotificationPages);
            _markNotificationsReadCommand = new RelayCommand(MarkCurrentNotificationsAsRead, () => Notifications.Any(item => item.IsUnread));
            _openNotificationCommand = new RelayCommand(OpenNotification, parameter => parameter is StaffNotification);
            _closeNotificationCommand = new RelayCommand(CloseNotification);

            RefreshStaffData();
            LoadNotifications();
        }

        public ObservableCollection<StaffTrackingMetric> Metrics { get; }
        public ObservableCollection<StaffPerformanceRow> StaffRows { get; }
        public ObservableCollection<StatusStat> DeadlineStats { get; }
        public ObservableCollection<StaffWorkRecord> ActiveRecords { get; }
        public ObservableCollection<StaffBarStat> BarStats { get; }
        public ObservableCollection<StaffNotification> Notifications { get; }
        public ObservableCollection<string> DateFilterOptions { get; }
        public ObservableCollection<string> Officers { get; }
        public ObservableCollection<string> LeadershipScopes { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand SaveLeadershipDirectiveCommand { get; }
        public ICommand SaveLeadershipKpiCommand { get; }
        public ICommand SelectMetricCommand { get; }
        public ICommand PreviousStaffPageCommand => _previousStaffPageCommand;
        public ICommand NextStaffPageCommand => _nextStaffPageCommand;
        public ICommand PreviousNotificationPageCommand => _previousNotificationPageCommand;
        public ICommand NextNotificationPageCommand => _nextNotificationPageCommand;
        public ICommand MarkNotificationsReadCommand => _markNotificationsReadCommand;
        public ICommand OpenNotificationCommand => _openNotificationCommand;
        public ICommand CloseNotificationCommand => _closeNotificationCommand;

        public bool CanSendLeadershipNotice => AuthContext.IsLeader || AuthContext.IsAdmin;
        public bool CanSetLeadershipKpi => AuthContext.IsLeader;
        public bool CanReadLeadershipNotice => AuthContext.IsOfficer || AuthContext.IsLeader;
        public bool CanReadAdminNotifications => AuthContext.IsLeader;
        public bool ShowStandaloneNotificationInbox => AuthContext.IsOfficer;
        public string LeadershipCardTitle => "THÔNG BÁO";
        public int LeadershipActionTabIndex => AuthContext.IsLeader ? 0 : 1;

        public string SelectedOfficer { get; set; }

        public StaffNotification SelectedNotification
        {
            get => _selectedNotification;
            private set => SetProperty(ref _selectedNotification, value);
        }

        public bool IsNotificationDetailOpen
        {
            get => _isNotificationDetailOpen;
            private set => SetProperty(ref _isNotificationDetailOpen, value);
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

        public int CurrentNotificationPage
        {
            get => _currentNotificationPage;
            private set
            {
                if (SetProperty(ref _currentNotificationPage, value))
                {
                    OnPropertyChanged(nameof(NotificationPageText));
                    RaiseNotificationCommandStates();
                }
            }
        }

        public int TotalNotificationPages
        {
            get => _totalNotificationPages;
            private set
            {
                if (SetProperty(ref _totalNotificationPages, value))
                {
                    OnPropertyChanged(nameof(NotificationPageText));
                    RaiseNotificationCommandStates();
                }
            }
        }

        public string NotificationPageText => $"Trang {CurrentNotificationPage}/{TotalNotificationPages}";

        public string NotificationSummaryText
        {
            get => _notificationSummaryText;
            private set => SetProperty(ref _notificationSummaryText, value);
        }

        public int UnreadNotificationCount
        {
            get => _unreadNotificationCount;
            private set => SetProperty(ref _unreadNotificationCount, value);
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

        public string LeadershipKpiStatus
        {
            get => _leadershipKpiStatus;
            set => SetProperty(ref _leadershipKpiStatus, value);
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
                var selectedValue = AuthContext.IsOfficer
                    ? _allStaffRows.FirstOrDefault(row => IsCurrentOfficer(row.Name)) ?? value
                    : value;

                if (SetProperty(ref _selectedStaff, selectedValue))
                {
                    RefreshLeadershipScopes();
                    LoadLeadershipKpiTarget();
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

            RefreshBarStats(_allStaffRows);
            RefreshMetricCards(_allStaffRows);
            RefreshDeadlineStats(deadlineStats);
            CurrentStaffPage = 1;
            ApplyMetricFilter();
        }

        public void Reload()
        {
            RefreshStaffData();
            LoadNotifications();
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

            SelectedOfficer = Officers.Contains(SelectedOfficer) ? SelectedOfficer : "Tất cả";
            SelectedStaff = AuthContext.IsOfficer
                ? _allStaffRows.FirstOrDefault(row => IsCurrentOfficer(row.Name))
                : StaffRows.FirstOrDefault(row => row.Name == SelectedOfficer) ?? StaffRows.FirstOrDefault();
            RaiseStaffPageCommandStates();
        }

        private void RefreshBarStats(IEnumerable<StaffPerformanceRow> staffRows)
        {
            BarStats.Clear();
            foreach (var row in staffRows)
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

        private void RaiseNotificationCommandStates()
        {
            _previousNotificationPageCommand?.RaiseCanExecuteChanged();
            _nextNotificationPageCommand?.RaiseCanExecuteChanged();
            _markNotificationsReadCommand?.RaiseCanExecuteChanged();
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
                string.Empty,
                LeadershipNoticeText);

            LeadershipActionStatus = $"Đã gửi thông báo cho {target} lúc {DateTime.Now:HH:mm}.";
            LeadershipNoticeText = string.Empty;
            NotifyUnreadCountChanged();
        }

        private void SaveLeadershipKpi()
        {
            if (!CanSetLeadershipKpi)
            {
                return;
            }

            var targetValue = GetLeadershipKpiTarget();
            if (targetValue <= 0)
            {
                LeadershipKpiStatus = "Vui lòng nhập chỉ tiêu KPI hợp lệ.";
                return;
            }

            var target = IsSelectedStaffLeadershipScope()
                ? SelectedStaff?.Name ?? "cán bộ đang chọn"
                : "toàn bộ cán bộ";

            var confirmation = MessageBox.Show(
                $"Bạn có chắc chắn muốn đặt KPI {targetValue} hồ sơ/tháng cho {target}?",
                "Xác nhận đặt KPI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var scope = IsSelectedStaffLeadershipScope() ? "Staff" : "All";
                var targetName = IsSelectedStaffLeadershipScope() ? SelectedStaff?.Name : string.Empty;
                AppDataService.Instance.SaveLeadershipKpi(
                    scope,
                    targetName,
                    LeadershipKpiTargetText);

                var currentLeaderName = (AuthContext.CurrentDisplayName ?? string.Empty).Trim();
                var leaderDisplayName = currentLeaderName.StartsWith("Lãnh đạo", StringComparison.CurrentCultureIgnoreCase)
                    ? currentLeaderName
                    : $"Lãnh đạo {currentLeaderName}";
                var notificationMessage = $"{leaderDisplayName} đã đặt KPI cho bạn là {targetValue} hồ sơ/tháng.";
                var notificationTargets = string.Equals(scope, "Staff", StringComparison.Ordinal)
                    ? new[] { targetName }
                    : _allStaffRows
                        .Select(row => row.Name?.Trim())
                        .Where(name => !string.IsNullOrWhiteSpace(name)
                            && !string.Equals(name, "Chưa có dữ liệu", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .ToArray();

                foreach (var notificationTarget in notificationTargets)
                {
                    AppDataService.Instance.SaveLeadershipNotice(
                        "Staff",
                        notificationTarget,
                        targetValue.ToString(CultureInfo.InvariantCulture),
                        notificationMessage);
                }

                LeadershipKpiStatus = $"Đã đặt KPI và gửi thông báo cho {target} lúc {DateTime.Now:HH:mm}.";
            }
            catch (InvalidOperationException ex)
            {
                LeadershipKpiStatus = $"Chưa lưu được KPI: {ex.Message}";
                return;
            }

            OnPropertyChanged(nameof(SelectedStaffCompletionText));
            OnPropertyChanged(nameof(SelectedStaffTargetProgressPercent));
            OnPropertyChanged(nameof(SelectedStaffTargetProgressText));
        }

        private void LoadLeadershipKpiTarget()
        {
            var targetStaffName = SelectedStaff?.Name;
            if (string.IsNullOrWhiteSpace(targetStaffName) || string.Equals(targetStaffName, "Chưa có dữ liệu", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            LeadershipKpiTargetText = AppDataService.Instance.GetLatestLeadershipKpiTarget(targetStaffName);
        }

        private void LoadNotifications()
        {
            if (!CanReadLeadershipNotice)
            {
                Notifications.Clear();
                NotificationSummaryText = string.Empty;
                UnreadNotificationCount = 0;
                _notificationUnreadCountChanged?.Invoke(0);
                return;
            }

            var page = AppDataService.Instance.GetLeadershipNotices(
                AuthContext.CurrentDisplayName,
                (CurrentNotificationPage - 1) * NotificationPageSize,
                NotificationPageSize,
                AuthContext.IsLeader);
            var totalCount = page?.TotalCount ?? 0;
            TotalNotificationPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)NotificationPageSize));
            if (CurrentNotificationPage > TotalNotificationPages)
            {
                CurrentNotificationPage = TotalNotificationPages;
                page = AppDataService.Instance.GetLeadershipNotices(
                    AuthContext.CurrentDisplayName,
                    (CurrentNotificationPage - 1) * NotificationPageSize,
                    NotificationPageSize,
                    AuthContext.IsLeader);
                totalCount = page?.TotalCount ?? 0;
            }

            Notifications.Clear();
            foreach (var notification in page?.Items ?? Array.Empty<StaffNotification>())
            {
                Notifications.Add(notification);
            }

            var skip = (CurrentNotificationPage - 1) * NotificationPageSize;
            var fromRow = totalCount == 0 ? 0 : skip + 1;
            var toRow = Math.Min(skip + NotificationPageSize, totalCount);
            UnreadNotificationCount = page?.UnreadCount ?? 0;
            NotificationSummaryText = totalCount == 0
                ? "Chưa có thông báo."
                : $"Hiển thị {fromRow} - {toRow} / {totalCount} thông báo";
            _notificationUnreadCountChanged?.Invoke(UnreadNotificationCount);
            RaiseNotificationCommandStates();
        }

        private void NextNotificationPage()
        {
            if (CurrentNotificationPage >= TotalNotificationPages)
            {
                return;
            }

            CurrentNotificationPage++;
            LoadNotifications();
        }

        private void PreviousNotificationPage()
        {
            if (CurrentNotificationPage <= 1)
            {
                return;
            }

            CurrentNotificationPage--;
            LoadNotifications();
        }

        private void MarkCurrentNotificationsAsRead()
        {
            var unreadIds = Notifications.Where(item => item.IsUnread).Select(item => item.Id).ToList();
            if (unreadIds.Count == 0)
            {
                return;
            }

            AppDataService.Instance.MarkLeadershipNoticesAsRead(AuthContext.CurrentDisplayName, unreadIds);
            LoadNotifications();
        }

        private void OpenNotification(object parameter)
        {
            if (parameter is not StaffNotification notification)
            {
                return;
            }

            SelectedNotification = notification;
            IsNotificationDetailOpen = true;

            if (!notification.IsUnread)
            {
                return;
            }

            AppDataService.Instance.MarkLeadershipNoticesAsRead(
                AuthContext.CurrentDisplayName,
                new[] { notification.Id });
            LoadNotifications();
        }

        private void CloseNotification()
        {
            IsNotificationDetailOpen = false;
            SelectedNotification = null;
        }

        private void NotifyUnreadCountChanged()
        {
            if (CanReadLeadershipNotice)
            {
                _notificationUnreadCountChanged?.Invoke(AppDataService.Instance.CountUnreadLeadershipNotices(
                    AuthContext.CurrentDisplayName,
                    AuthContext.IsLeader));
            }
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
