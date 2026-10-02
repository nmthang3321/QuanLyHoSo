using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Documents;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using System.Windows.Threading;
using QuanLyHoSo.ApplicationServices.Abstractions;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordProcessingViewModel : ViewModelBase
    {
        private const int DefaultPageSize = 20;
        private const int MinimumPageSize = 1;
        private const int MaximumPageSize = 50;
        private const int AssignedProcessStep = 3;
        private const string ThisWeekFilter = "Tuần này";
        private const string ThisMonthFilter = "Tháng này";
        private const string ThisYearFilter = "Năm này";
        private const string CustomFilter = "Khác";

        private readonly IApplicationDataService _dataService;
        private readonly Action _goBackToPreviousPage;
        private readonly RelayCommand _nextPageCommand;
        private readonly RelayCommand _previousPageCommand;
        private readonly RelayCommand _saveAttachmentChangesCommand;
        private readonly DispatcherTimer _searchDebounceTimer;
        private readonly Dictionary<string, string> _editableAttachmentPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private int _currentPage = 1;
        private int _pageSize = DefaultPageSize;
        private string _pageSizeText = DefaultPageSize.ToString(CultureInfo.InvariantCulture);
        private string _searchText;
        private RecordFormDraft _selectedRecordDetail;
        private ProcessingRecordDetail _selectedProcessingDetail;
        private DateTime? _selectedProcessingDate;
        private string _processingContent;
        private string _processingProcessorName;
        private string _processingStatus;
        private string _resubmissionNote = string.Empty;
        private string _transferAreaName;
        private string _transferAreaSearchText;
        private string _selectedArea;
        private string _selectedMetricKey = "All";
        private string _selectedSeverity;
        private string _selectedStatus;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedDateFilter;
        private bool _isCustomCalendarOpen;
        private bool _shouldReturnToPreviousPage;
        private bool _isProcessingUpdateBusy;
        private bool _isLoading;
        private bool _reloadRequested;
        private bool _isDocumentDetailsOpen;
        private string _documentTransferNumber;
        private DateTime? _documentTransferDate;
        private DateTime? _documentComplaintDate;
        private string _documentReview;
        private string _documentProposal;
        private string _documentCommanderApproverName;
        private string _documentLeaderApproverName;
        private string _documentDetailsError;
        private RecordFormDraft _documentPreviewRecord;
        private int _totalPages = 1;
        private string _totalRecordsText;

        public RecordProcessingViewModel(Action goBackToPreviousPage = null)
            : this(AppDataService.Instance, goBackToPreviousPage)
        {
        }

        public RecordProcessingViewModel(IApplicationDataService dataService, Action goBackToPreviousPage = null)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _goBackToPreviousPage = goBackToPreviousPage ?? (() => { });
            var today = DateTime.Today;
            _fromDate = new DateTime(today.Year, 1, 1);
            _toDate = new DateTime(today.Year, 12, 31);
            _selectedDateFilter = ThisYearFilter;
            Metrics = new ObservableCollection<DashboardMetric>();
            QueueRecords = new ObservableCollection<ProcessingQueueRecord>();
            ProcessSteps = new ObservableCollection<ProcessStep>();
            History = new ObservableCollection<ProcessHistoryItem>();
            var processorNamesTask = Task.Run(() => _dataService.GetProcessorNames());
            var transferAreasTask = Task.Run(() => _dataService.GetAreaNames());
            var areaFiltersTask = Task.Run(() => _dataService.GetAreaNames(includeAll: true));
            var severityFiltersTask = Task.Run(() => _dataService.GetCatalogValues("Priority", includeAll: true));
            Task.WhenAll(processorNamesTask, transferAreasTask, areaFiltersTask, severityFiltersTask).GetAwaiter().GetResult();

            ProcessorNames = new ObservableCollection<string>(processorNamesTask.Result);
            TransferAreas = AreaSelectionOptions.Build(transferAreasTask.Result, includeGroupRows: true, groupRowsSelectable: false);
            FilteredTransferAreas = AreaSelectionOptions.Filter(TransferAreas, null);
            Attachments = new ObservableCollection<AttachmentDraft>();
            ProcessingStatuses = new ObservableCollection<string>(GetAllowedProcessingStatuses());
            StatusFilters = new ObservableCollection<string>
            {
                "Tất cả",
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Kết quả xử lý ban đầu",
                "Chuyển cơ quan khác",
                "Chờ kết quả"
            };
            AreaFilters = AreaSelectionOptions.Build(areaFiltersTask.Result, includeGroupRows: true, groupRowsSelectable: true);
            SeverityFilters = new ObservableCollection<string>(severityFiltersTask.Result);
            DateFilterOptions = new ObservableCollection<string>
            {
                ThisWeekFilter,
                ThisMonthFilter,
                ThisYearFilter,
                CustomFilter
            };
            _dataService.CatalogChanged += DataService_CatalogChanged;
            ApplyFilterCommand = new RelayCommand(ReloadFromFirstPage);
            ViewRecordCommand = new RelayCommand(ViewRecord);
            ViewProcessingDetailCommand = new RelayCommand(ViewProcessingDetail);
            OpenProcessingDetailCommand = new RelayCommand(OpenProcessingDetail);
            SelectMetricCommand = new RelayCommand(SelectMetric);
            _previousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            _nextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
            CloseDetailCommand = new RelayCommand(CloseDetail);
            BackToQueueCommand = new RelayCommand(BackToQueue);
            SaveProcessingCommand = new RelayCommand(async () => await SaveProcessingAsync(), () => CanUpdateProcessing && !IsProcessingUpdateBusy);
            ConfirmDocumentDetailsCommand = new RelayCommand(async () => await ConfirmDocumentDetailsAsync(), () => CanUpdateProcessing && !IsProcessingUpdateBusy);
            CancelDocumentDetailsCommand = new RelayCommand(() => IsDocumentDetailsOpen = false, () => !IsProcessingUpdateBusy);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            OpenAttachmentCommand = new RelayCommand(OpenAttachment);
            DownloadAttachmentCommand = new RelayCommand(DownloadAttachment);
            _saveAttachmentChangesCommand = new RelayCommand(async parameter => await SaveAttachmentChangesAsync(parameter), CanSaveAttachmentChanges);
            SaveAttachmentChangesCommand = _saveAttachmentChangesCommand;

            _selectedStatus = StatusFilters[0];
            _selectedArea = AreaFilters.Count > 0 ? AreaFilters[0].FilterValue : "Tất cả";
            _selectedSeverity = SeverityFilters.Count > 0 ? SeverityFilters[0] : "Tất cả";

            Reload();
        }

        public ObservableCollection<DashboardMetric> Metrics { get; }
        public ObservableCollection<ProcessingQueueRecord> QueueRecords { get; }
        public ObservableCollection<ProcessStep> ProcessSteps { get; }
        public ObservableCollection<ProcessHistoryItem> History { get; }
        public ObservableCollection<string> ProcessorNames { get; }
        public ObservableCollection<AreaSelectionOption> TransferAreas { get; }
        public ObservableCollection<AreaSelectionOption> FilteredTransferAreas { get; }
        public ObservableCollection<AttachmentDraft> Attachments { get; }
        public bool HasAttachments => Attachments.Count > 0;
        public ObservableCollection<string> ProcessingStatuses { get; }
        public ObservableCollection<string> StatusFilters { get; }
        public ObservableCollection<AreaSelectionOption> AreaFilters { get; }
        public ObservableCollection<string> SeverityFilters { get; }
        public ObservableCollection<string> DateFilterOptions { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ViewRecordCommand { get; }
        public ICommand ViewProcessingDetailCommand { get; }
        public ICommand OpenProcessingDetailCommand { get; }
        public ICommand SelectMetricCommand { get; }
        public ICommand PreviousPageCommand => _previousPageCommand;
        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand CloseDetailCommand { get; }
        public ICommand BackToQueueCommand { get; }
        public ICommand SaveProcessingCommand { get; }
        public ICommand ConfirmDocumentDetailsCommand { get; }
        public ICommand CancelDocumentDetailsCommand { get; }
        public bool IsDocumentDetailsOpen { get => _isDocumentDetailsOpen; private set => SetProperty(ref _isDocumentDetailsOpen, value); }
        public string DocumentTransferNumber { get => _documentTransferNumber; set => SetProperty(ref _documentTransferNumber, value); }
        public DateTime? DocumentTransferDate { get => _documentTransferDate; set => SetProperty(ref _documentTransferDate, value); }
        public DateTime? DocumentComplaintDate { get => _documentComplaintDate; set => SetProperty(ref _documentComplaintDate, value); }
        public string DocumentReview { get => _documentReview; set => SetProperty(ref _documentReview, value); }
        public string DocumentProposal { get => _documentProposal; set => SetProperty(ref _documentProposal, value); }
        public string DocumentCommanderApproverName { get => _documentCommanderApproverName; set => SetProperty(ref _documentCommanderApproverName, value); }
        public string DocumentLeaderApproverName { get => _documentLeaderApproverName; set => SetProperty(ref _documentLeaderApproverName, value); }
        public string DocumentDetailsError { get => _documentDetailsError; private set => SetProperty(ref _documentDetailsError, value); }
        public RecordFormDraft DocumentPreviewRecord { get => _documentPreviewRecord; private set => SetProperty(ref _documentPreviewRecord, value); }
        public ICommand RemoveAttachmentCommand { get; }
        public ICommand OpenAttachmentCommand { get; }
        public ICommand DownloadAttachmentCommand { get; }
        public ICommand SaveAttachmentChangesCommand { get; }
        public string TransferAreaSearchText
        {
            get => _transferAreaSearchText;
            set
            {
                if (SetProperty(ref _transferAreaSearchText, value))
                {
                    ReplaceItems(FilteredTransferAreas, AreaSelectionOptions.Filter(TransferAreas, value));

                    var exactMatch = AreaSelectionOptions.Flatten(TransferAreas)
                        .FirstOrDefault(area => area.IsSelectable && string.Equals(area.DisplayName, value, StringComparison.CurrentCultureIgnoreCase));
                    if (exactMatch != null && !string.Equals(_transferAreaName, exactMatch.FilterValue, StringComparison.Ordinal))
                    {
                        _transferAreaName = exactMatch.FilterValue;
                        OnPropertyChanged(nameof(TransferAreaName));
                        OnPropertyChanged(nameof(TransferAreaDisplayName));
                    }
                }
            }
        }

        public string TransferAreaName
        {
            get => _transferAreaName;
            set
            {
                if (!SetProperty(ref _transferAreaName, value))
                {
                    return;
                }

                if (!string.Equals(_transferAreaSearchText, value, StringComparison.Ordinal))
                {
                    TransferAreaSearchText = value;
                }

                OnPropertyChanged(nameof(TransferAreaDisplayName));
            }
        }

        public string TransferAreaDisplayName => string.IsNullOrWhiteSpace(TransferAreaName)
            ? "Chọn địa bàn"
            : AreaSelectionOptions.GetDisplayName(TransferAreas, TransferAreaName);
        public bool CanUpdateProcessing => SelectedProcessingDetail != null && SelectedProcessingDetail.Status != RecordStatuses.ResubmittedResolved && AuthContext.CanEditRecord(SelectedProcessingDetail.ProcessorName);

        public bool CanChangeProcessor => AuthContext.IsAdmin;

        public bool IsProcessingUpdateBusy
        {
            get => _isProcessingUpdateBusy;
            private set
            {
                if (SetProperty(ref _isProcessingUpdateBusy, value))
                {
                    (SaveProcessingCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ConfirmDocumentDetailsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CancelDocumentDetailsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    _saveAttachmentChangesCommand?.RaiseCanExecuteChanged();
                }
            }
        }

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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Start();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    ReloadFromFirstPage();
                }
            }
        }

        public string SelectedArea
        {
            get => _selectedArea;
            set
            {
                if (SetProperty(ref _selectedArea, value))
                {
                    OnPropertyChanged(nameof(SelectedAreaDisplayName));
                    ReloadFromFirstPage();
                }
            }
        }

        public string SelectedAreaDisplayName => AreaSelectionOptions.GetDisplayName(AreaFilters, SelectedArea);

        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set
            {
                if (SetProperty(ref _selectedSeverity, value))
                {
                    ReloadFromFirstPage();
                }
            }
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
                var culture = CultureInfo.GetCultureInfo("vi-VN");
                var fromText = FromDate?.ToString("dd/MM/yyyy", culture) ?? "--/--/----";
                var toText = ToDate?.ToString("dd/MM/yyyy", culture) ?? "--/--/----";
                return $"{SelectedDateFilter} ({fromText} - {toText})";
            }
        }

        public bool IsCustomCalendarOpen
        {
            get => _isCustomCalendarOpen;
            set => SetProperty(ref _isCustomCalendarOpen, value);
        }

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

        public ProcessingRecordDetail SelectedProcessingDetail
        {
            get => _selectedProcessingDetail;
            private set
            {
                if (SetProperty(ref _selectedProcessingDetail, value))
                {
                    OnPropertyChanged(nameof(IsProcessingDetailOpen));
                    OnPropertyChanged(nameof(CanUpdateProcessing));
                    (SaveProcessingCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    _saveAttachmentChangesCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsProcessingDetailOpen => SelectedProcessingDetail != null;

        public string ResubmissionNote
        {
            get => _resubmissionNote;
            private set
            {
                if (SetProperty(ref _resubmissionNote, value))
                {
                    OnPropertyChanged(nameof(HasResubmissionNote));
                }
            }
        }

        public bool HasResubmissionNote => !string.IsNullOrWhiteSpace(ResubmissionNote);

        public string ProcessingStatus
        {
            get => _processingStatus;
            set => SetProperty(ref _processingStatus, value);
        }

        public DateTime? SelectedProcessingDate
        {
            get => _selectedProcessingDate;
            set => SetProperty(ref _selectedProcessingDate, value);
        }

        public string ProcessingProcessorName
        {
            get => _processingProcessorName;
            set => SetProperty(ref _processingProcessorName, value);
        }

        public string ProcessingContent
        {
            get => _processingContent;
            set => SetProperty(ref _processingContent, value);
        }

        public async void Reload()
        {
            if (_isLoading)
            {
                _reloadRequested = true;
                return;
            }

            _isLoading = true;
            var searchText = SearchText;
            var status = SelectedStatus;
            var area = SelectedArea;
            var severity = SelectedSeverity;
            var metricKey = _selectedMetricKey;
            var fromDate = FromDate;
            var toDate = ToDate;
            var pageSize = _pageSize;
            var requestedPage = CurrentPage;
            var requestedSkip = (requestedPage - 1) * pageSize;

            try
            {
                var metricsTask = Task.Run(() => _dataService.GetProcessingQueueMetrics(fromDate, toDate));
                var countTask = Task.Run(() => _dataService.CountProcessingQueueRecords(
                    searchText, status, area, severity, metricKey, fromDate, toDate));
                var recordsTask = Task.Run(() => _dataService.GetProcessingQueueRecords(
                    searchText, status, area, severity, metricKey, requestedSkip, pageSize, fromDate, toDate));
                await Task.WhenAll(metricsTask, countTask, recordsTask);

                var totalRecords = countTask.Result;
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
                var page = Math.Min(requestedPage, totalPages);
                var records = recordsTask.Result;
                if (page != requestedPage)
                {
                    records = await Task.Run(() => _dataService.GetProcessingQueueRecords(
                        searchText, status, area, severity, metricKey, (page - 1) * pageSize, pageSize, fromDate, toDate));
                }

                if (IsDisposed)
                {
                    return;
                }

                var metrics = metricsTask.Result;
                UpdateMetricSelection(metrics);
                ReplaceItems(Metrics, metrics);
                TotalRecordsText = $"{totalRecords:N0} hồ sơ phù hợp";
                TotalPages = totalPages;
                CurrentPage = page;
                ReplaceItems(QueueRecords, records);
                RaisePageCommandStates();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Processing", "Reload", ex, "Could not reload the processing queue.");
            }
            finally
            {
                _isLoading = false;
                if (_reloadRequested && !IsDisposed)
                {
                    _reloadRequested = false;
                    Reload();
                }
            }
        }

        private void ReloadFromFirstPage()
        {
            _searchDebounceTimer.Stop();
            CurrentPage = 1;
            Reload();
        }

        private void SearchDebounceTimer_Tick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            ReloadFromFirstPage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataService.CatalogChanged -= DataService_CatalogChanged;
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
            }

            base.Dispose(disposing);
        }

        private void LoadPage()
        {
            var skip = (CurrentPage - 1) * _pageSize;
            ReplaceItems(QueueRecords, _dataService.GetProcessingQueueRecords(
                SearchText,
                SelectedStatus,
                SelectedArea,
                SelectedSeverity,
                _selectedMetricKey,
                skip,
                _pageSize,
                FromDate,
                ToDate));
            RaisePageCommandStates();
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

        private void SelectMetric(object parameter)
        {
            if (parameter is not DashboardMetric metric || string.IsNullOrWhiteSpace(metric.FilterKey))
            {
                return;
            }

            _selectedMetricKey = metric.FilterKey;
            _selectedStatus = StatusFilters.Count > 0 ? StatusFilters[0] : "Tất cả";
            _selectedArea = AreaFilters.Count > 0 ? AreaFilters[0].FilterValue : "Tất cả";
            _selectedSeverity = SeverityFilters.Count > 0 ? SeverityFilters[0] : "Tất cả";
            _searchText = string.Empty;
            OnPropertyChanged(nameof(SelectedStatus));
            OnPropertyChanged(nameof(SelectedArea));
            OnPropertyChanged(nameof(SelectedSeverity));
            OnPropertyChanged(nameof(SearchText));
            ReloadFromFirstPage();
        }

        private void UpdateMetricSelection(IEnumerable<DashboardMetric> metrics)
        {
            foreach (var metric in metrics)
            {
                metric.IsSelected = string.Equals(metric.FilterKey, _selectedMetricKey, StringComparison.Ordinal);
            }
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

            ReloadFromFirstPage();
        }

        private void ViewRecord(object parameter)
        {
            if (parameter is ProcessingQueueRecord record)
            {
                SelectedRecordDetail = _dataService.GetRecordForm(record.RecordCode);
            }
        }

        private void ViewProcessingDetail()
        {
            if (!string.IsNullOrWhiteSpace(SelectedProcessingDetail?.RecordCode))
            {
                SelectedRecordDetail = _dataService.GetRecordForm(SelectedProcessingDetail.RecordCode);
            }
        }

        private void OpenProcessingDetail(object parameter)
        {
            if (parameter is not ProcessingQueueRecord record)
            {
                return;
            }

            OpenProcessingDetail(record.RecordCode);
        }

        public void OpenRecord(string recordCode, bool returnToPreviousPage = false, string viaResubmissionCode = null)
        {
            _shouldReturnToPreviousPage = returnToPreviousPage;
            OpenProcessingDetail(recordCode, viaResubmissionCode);
        }

        private void OpenProcessingDetail(string recordCode, string viaResubmissionCode = null)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return;
            }

            IsDocumentDetailsOpen = false;
            ResubmissionNote = string.Empty;
            DocumentTransferNumber = string.Empty;
            DocumentTransferDate = null;
            DocumentComplaintDate = null;
            DocumentReview = string.Empty;
            DocumentProposal = string.Empty;
            DocumentCommanderApproverName = string.Empty;
            DocumentLeaderApproverName = string.Empty;
            DocumentDetailsError = string.Empty;
            DocumentPreviewRecord = null;
            SelectedProcessingDetail = _dataService.GetProcessingRecordDetail(recordCode);
            ReplaceItems(ProcessorNames, _dataService.GetProcessorNames());
            ReplaceItems(ProcessSteps, NormalizeProcessSteps(SelectedProcessingDetail.Steps));
            ReplaceItems(History, NormalizeProcessHistory(SelectedProcessingDetail.History));
            ReplaceItems(ProcessingStatuses, GetAllowedProcessingStatuses());
            ProcessingStatus = SelectedProcessingDetail.Status;
            if (!ProcessingStatuses.Contains(ProcessingStatus))
            {
                ProcessingStatus = ProcessingStatuses.FirstOrDefault() ?? SelectedProcessingDetail.Status;
            }
            SelectedProcessingDate = ParseProcessingDate(SelectedProcessingDetail.ProcessingDate) ?? DateTime.Now;
            ProcessingProcessorName = AuthContext.IsAdmin
                ? SelectedProcessingDetail.ProcessorName
                : AuthContext.CurrentDisplayName;
            ProcessingContent = string.Empty;
            TransferAreaName = SelectedProcessingDetail.AreaName;
            TransferAreaSearchText = SelectedProcessingDetail.AreaName;
            LoadProcessingAttachments(SelectedProcessingDetail);
            ResubmissionNote = string.IsNullOrWhiteSpace(viaResubmissionCode)
                ? string.Empty
                : $"Bạn đang xử lý trên hồ sơ gốc {SelectedProcessingDetail.RecordCode} thông qua hồ sơ gửi lại {viaResubmissionCode}.";
        }

        private void DataService_CatalogChanged(string catalogType)
        {
            switch (catalogType)
            {
                case "Priority":
                    ReplaceItems(SeverityFilters, _dataService.GetCatalogValues(catalogType, includeAll: true));
                    if (!string.IsNullOrWhiteSpace(SelectedSeverity) && !SeverityFilters.Contains(SelectedSeverity))
                    {
                        SelectedSeverity = SeverityFilters.Count > 0 ? SeverityFilters[0] : null;
                        Reload();
                    }
                    break;
                case "ProcessorName":
                    ReplaceItems(ProcessorNames, _dataService.GetProcessorNames());
                    if (!string.IsNullOrWhiteSpace(ProcessingProcessorName) && !ProcessorNames.Contains(ProcessingProcessorName))
                    {
                        ProcessingProcessorName = null;
                    }
                    break;
            }
        }

        private void CloseDetail()
        {
            SelectedRecordDetail = null;
        }

        public void PrepareQueue()
        {
            IsDocumentDetailsOpen = false;
            DocumentPreviewRecord = null;
            _shouldReturnToPreviousPage = false;
            SelectedRecordDetail = null;
            SelectedProcessingDetail = null;
            ProcessSteps.Clear();
            History.Clear();
            Attachments.Clear();
            _editableAttachmentPaths.Clear();
            _saveAttachmentChangesCommand?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(HasAttachments));
        }

        private void BackToQueue()
        {
            var returnToPreviousPage = _shouldReturnToPreviousPage;
            PrepareQueue();
            if (returnToPreviousPage)
            {
                _goBackToPreviousPage();
                return;
            }

            Reload();
        }

        private async Task ConfirmDocumentDetailsAsync()
        {
            var details = new InitialResultDocumentDetails
            {
                TransferNumber = DocumentTransferNumber,
                TransferDate = DocumentTransferDate,
                ComplaintDate = DocumentComplaintDate,
                Review = DocumentReview,
                Proposal = DocumentProposal,
                CommanderApproverName = DocumentCommanderApproverName,
                ProposingOfficerName = ProcessingProcessorName,
                LeaderApproverName = DocumentLeaderApproverName
            };
            DocumentDetailsError = details.GetValidationMessage();
            if (!string.IsNullOrEmpty(DocumentDetailsError)) return;
            await SaveProcessingAsync(details);
        }

        private async Task SaveProcessingAsync(InitialResultDocumentDetails documentDetails = null)
        {
            if (!CanUpdateProcessing)
            {
                MessageBox.Show("Bạn chỉ được cập nhật hồ sơ đứng dưới tên mình. Tài khoản lãnh đạo chỉ được xem.", "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProcessingDetail == null)
            {
                return;
            }

            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(ProcessingStatus))
            {
                missingFields.Add("Trạng thái hiện tại");
            }
            else if (!CanSelectProcessingStatus(ProcessingStatus))
            {
                MessageBox.Show("Cán bộ không được lùi quy trình về Mới tiếp nhận hoặc Đang phân loại. Vui lòng chọn trạng thái từ Đã phân công trở đi.", "Phân quyền quy trình", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!SelectedProcessingDate.HasValue)
            {
                missingFields.Add("Ngày xử lý");
            }

            if (string.IsNullOrWhiteSpace(ProcessingProcessorName))
            {
                missingFields.Add("Người xử lý");
            }

            if (string.IsNullOrWhiteSpace(ProcessingContent))
            {
                missingFields.Add("Nội dung xử lý");
            }

            if (string.Equals(ProcessingStatus, "Chuyển cơ quan khác", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(TransferAreaName))
            {
                missingFields.Add("Cơ quan chuyển đến");
            }

            if (missingFields.Count > 0)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin bắt buộc:\n\n- " + string.Join("\n- ", missingFields), "Thiếu thông tin xử lý", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var recordCode = SelectedProcessingDetail.RecordCode;
            var generateInitialResultDocuments = documentDetails != null || (ShouldOfferInitialResultDocuments()
                && MessageBox.Show(
                    "Hồ sơ chưa có đủ phiếu đề xuất, phiếu hướng dẫn và thông báo. Bạn có muốn tạo các file còn thiếu không?",
                    "Tạo tài liệu kết quả xử lý ban đầu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes);
            if (generateInitialResultDocuments && documentDetails == null)
            {
                try
                {
                    IsProcessingUpdateBusy = true;
                    var preview = await Task.Run(() => _dataService.GetRecordForm(recordCode));
                    if (preview == null) throw new InvalidOperationException("Không tìm thấy thông tin hồ sơ để xem trước.");
                    DocumentPreviewRecord = preview;
                    DocumentReview = string.Empty;
                    DocumentProposal = string.Empty;
                    DocumentCommanderApproverName = string.Empty;
                    DocumentLeaderApproverName = string.Empty;
                    DocumentDetailsError = string.Empty;
                    IsDocumentDetailsOpen = true;
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Documents", "PreviewInitialResultDocuments", ex, "Failed to load document preview.", recordCode);
                    MessageBox.Show("Không thể tải thông tin xem trước. Vui lòng thử lại.", "Tạo tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    IsProcessingUpdateBusy = false;
                }
                return;
            }
            var attachmentsToSave = Attachments.ToList();
            var selectedStatus = ProcessingStatus;
            var selectedTransferArea = string.Equals(selectedStatus, "Chuyển cơ quan khác", StringComparison.Ordinal)
                ? TransferAreaName
                : null;
            try
            {
                IsProcessingUpdateBusy = true;
                ProcessingRecordDetail refreshedDetail = null;
                await Task.Run(() =>
                {
                    _dataService.UpdateProcessingRecord(
                        recordCode,
                        selectedStatus,
                        SelectedProcessingDate.Value,
                        ProcessingProcessorName,
                        ProcessingContent,
                        string.Empty, // Ghi chú đã bị bỏ khỏi giao diện; Note cũ trên record được giữ nguyên.
                        selectedTransferArea,
                        attachmentsToSave,
                        generateInitialResultDocuments,
                        documentDetails);

                    refreshedDetail = _dataService.GetProcessingRecordDetail(recordCode);
                    if (string.Equals(selectedStatus, "Chuyển cơ quan khác", StringComparison.Ordinal)
                        && string.Equals(refreshedDetail?.Status, "Chuyển cơ quan khác", StringComparison.Ordinal))
                    {
                        // Compatibility with an older server: record the transfer first,
                        // then advance it to the state that waits for the receiving agency.
                        _dataService.UpdateProcessingRecord(
                            recordCode,
                            "Chờ kết quả",
                            SelectedProcessingDate.Value,
                            ProcessingProcessorName,
                            ProcessingContent,
                            string.Empty,
                            selectedTransferArea,
                            attachmentsToSave);
                        refreshedDetail = _dataService.GetProcessingRecordDetail(recordCode);
                    }
                });

                AppLogger.Info("Processing", "UpdateProcessingRecord", "Processing record updated.", recordCode);
                ApplyProcessingDetail(refreshedDetail);
                IsDocumentDetailsOpen = false;
                MessageBox.Show(
                    generateInitialResultDocuments ? "Đã cập nhật xử lý hồ sơ và tạo tài liệu liên quan." : "Đã cập nhật xử lý hồ sơ.",
                    "Cập nhật xử lý",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Processing", "UpdateProcessingRecord", ex, "Failed to update processing record.", recordCode);
                MessageBox.Show($"Không thể cập nhật xử lý hồ sơ.\n\nChi tiết: {ex.Message}", "Lỗi cập nhật xử lý", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessingUpdateBusy = false;
            }
        }

        public void AddAttachmentFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths ?? Array.Empty<string>())
            {
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024 || !new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" }.Contains(fileInfo.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Attachments.Any(item => string.Equals(item.FileName, fileInfo.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                Attachments.Add(new AttachmentDraft
                {
                    FileName = fileInfo.Name,
                    FileSize = FormatFileSize(fileInfo.Length),
                    FilePath = fileInfo.FullName,
                    Content = File.ReadAllBytes(fileInfo.FullName)
                });
            }

            OnPropertyChanged(nameof(HasAttachments));
        }

        private void RemoveAttachment(object parameter)
        {
            if (parameter is AttachmentDraft attachment)
            {
                Attachments.Remove(attachment);
                OnPropertyChanged(nameof(HasAttachments));
            }
        }

        private void OpenAttachment(object parameter)
        {
            if (parameter is not AttachmentDraft attachment || string.IsNullOrWhiteSpace(attachment.FileName))
            {
                MessageBox.Show("Tài liệu này chưa có đường dẫn file để mở.", "Tải tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string localPath;
                if (attachment.Content != null && File.Exists(attachment.FilePath))
                {
                    localPath = attachment.FilePath;
                    if (attachment.CanSaveEditedContent)
                    {
                        _editableAttachmentPaths[GetAttachmentEditKey(attachment.FileName)] = localPath;
                        _saveAttachmentChangesCommand.RaiseCanExecuteChanged();
                    }
                }
                else if (CanUpdateProcessing && attachment.CanSaveEditedContent)
                {
                    var editKey = GetAttachmentEditKey(attachment.FileName);
                    if (!_editableAttachmentPaths.TryGetValue(editKey, out localPath) || !File.Exists(localPath))
                    {
                        var editFolder = Path.Combine(
                            Path.GetTempPath(),
                            "QuanLyHoSo",
                            "AttachmentEdits",
                            Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
                            SanitizePathSegment(SelectedProcessingDetail?.RecordCode, "record"));
                        Directory.CreateDirectory(editFolder);
                        localPath = Path.Combine(editFolder, Path.GetFileName(attachment.FileName));
                        _dataService.DownloadAttachment(SelectedProcessingDetail?.RecordCode, attachment.FileName, localPath);
                        _editableAttachmentPaths[editKey] = localPath;
                        _saveAttachmentChangesCommand.RaiseCanExecuteChanged();
                    }
                }
                else
                {
                    localPath = _dataService.GetAttachmentFilePath(SelectedProcessingDetail?.RecordCode, attachment.FileName);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = localPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở tài liệu.\n{ex.Message}", "Tải tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanSaveAttachmentChanges(object parameter)
        {
            if (!CanUpdateProcessing || IsProcessingUpdateBusy || parameter is not AttachmentDraft attachment || !attachment.CanSaveEditedContent)
            {
                return false;
            }

            return _editableAttachmentPaths.TryGetValue(GetAttachmentEditKey(attachment.FileName), out var localPath)
                && File.Exists(localPath);
        }

        private async Task SaveAttachmentChangesAsync(object parameter)
        {
            if (parameter is not AttachmentDraft attachment || !CanSaveAttachmentChanges(attachment))
            {
                MessageBox.Show("Hãy mở tài liệu trước, chỉnh sửa và bấm Save trong Word/Excel rồi chọn Lưu thay đổi.", "Lưu tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var recordCode = SelectedProcessingDetail?.RecordCode;
            var editKey = GetAttachmentEditKey(attachment.FileName);
            var localPath = _editableAttachmentPaths[editKey];
            try
            {
                var fileInfo = new FileInfo(localPath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Tài liệu vượt quá dung lượng tối đa 10 MB.", "Lưu tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsProcessingUpdateBusy = true;
                var content = await Task.Run(() => File.ReadAllBytes(localPath));
                var updatedAttachment = attachment.Content != null
                    ? new AttachmentDraft
                    {
                        FileName = attachment.FileName,
                        FileSize = FormatFileSize(fileInfo.Length),
                        FilePath = localPath,
                        Content = content
                    }
                    : await Task.Run(() => _dataService.UpdateAttachmentContent(recordCode, attachment.FileName, content));
                var index = Attachments.IndexOf(attachment);
                if (index >= 0)
                {
                    Attachments[index] = updatedAttachment;
                    _editableAttachmentPaths[GetAttachmentEditKey(updatedAttachment.FileName)] = localPath;
                }

                if (SelectedProcessingDetail?.Attachments != null)
                {
                    SelectedProcessingDetail.Attachments = Attachments.ToList();
                }

                AppLogger.Info("Attachments", "UpdateContent", "Attachment content updated.", recordCode);
                var successMessage = attachment.Content == null
                    ? "Đã lưu bản chỉnh sửa lên hệ thống."
                    : "Đã cập nhật bản chỉnh sửa trong danh sách tài liệu. Bấm Cập nhật để lưu xử lý hồ sơ.";
                MessageBox.Show(successMessage, "Lưu tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Không thể đọc tài liệu. Hãy bấm Save và đóng tài liệu trong ứng dụng đang mở rồi thử lại.\n\nChi tiết: {ex.Message}", "Lưu tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Attachments", "UpdateContent", ex, "Failed to update attachment content.", recordCode);
                MessageBox.Show($"Không thể lưu bản chỉnh sửa lên hệ thống.\n\nChi tiết: {ex.Message}", "Lưu tài liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessingUpdateBusy = false;
                _saveAttachmentChangesCommand.RaiseCanExecuteChanged();
            }
        }

        private string GetAttachmentEditKey(string fileName)
        {
            return $"{SelectedProcessingDetail?.RecordCode}|{Path.GetFileName(fileName ?? string.Empty)}";
        }

        private static string SanitizePathSegment(string value, string fallback)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var sanitized = new string((value ?? string.Empty)
                .Trim()
                .Select(character => invalidCharacters.Contains(character) ? '_' : character)
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        private void DownloadAttachment(object parameter)
        {
            if (parameter is not AttachmentDraft attachment || string.IsNullOrWhiteSpace(attachment.FileName))
            {
                MessageBox.Show("Tài liệu này chưa có đường dẫn file để tải về.", "Tải tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = attachment.FileName,
                Filter = BuildDownloadFilter(attachment.FileName),
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (attachment.Content != null && File.Exists(attachment.FilePath))
                {
                    File.Copy(attachment.FilePath, dialog.FileName, true);
                }
                else
                {
                    _dataService.DownloadAttachment(SelectedProcessingDetail?.RecordCode, attachment.FileName, dialog.FileName);
                }
                MessageBox.Show("Đã tải tài liệu về máy.", "Tải tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải tài liệu.\n{ex.Message}", "Tải tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string BuildDownloadFilter(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".doc" or ".docx" => "Word (*.doc;*.docx)|*.doc;*.docx|Tất cả tệp (*.*)|*.*",
                ".pdf" => "PDF (*.pdf)|*.pdf|Tất cả tệp (*.*)|*.*",
                ".jpg" or ".jpeg" => "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*",
                ".png" => "PNG (*.png)|*.png|Tất cả tệp (*.*)|*.*",
                _ => "Tất cả tệp (*.*)|*.*"
            };
        }

        private static string FormatFileSize(long bytes)
        {
            return bytes < 1024 * 1024
                ? $"{Math.Max(1, bytes / 1024)} KB"
                : $"{bytes / (1024d * 1024d):0.0} MB";
        }

        private static DateTime? ParseProcessingDate(string value)
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var exactDate))
            {
                return exactDate;
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                ? date
                : (DateTime?)null;
        }

        private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void ApplyProcessingDetail(ProcessingRecordDetail detail)
        {
            SelectedProcessingDetail = detail;
            ReplaceItems(ProcessSteps, NormalizeProcessSteps(SelectedProcessingDetail.Steps));
            ReplaceItems(History, NormalizeProcessHistory(SelectedProcessingDetail.History));
            ReplaceItems(ProcessingStatuses, GetAllowedProcessingStatuses());
            ReplaceItems(ProcessorNames, _dataService.GetProcessorNames());
            ProcessingStatus = SelectedProcessingDetail.Status;
            if (!ProcessingStatuses.Contains(ProcessingStatus))
            {
                ProcessingStatus = ProcessingStatuses.FirstOrDefault() ?? SelectedProcessingDetail.Status;
            }

            ProcessingProcessorName = AuthContext.IsAdmin
                ? SelectedProcessingDetail.ProcessorName
                : AuthContext.CurrentDisplayName;
            TransferAreaName = SelectedProcessingDetail.AreaName;
            TransferAreaSearchText = SelectedProcessingDetail.AreaName;
            LoadProcessingAttachments(SelectedProcessingDetail);
        }

        private void LoadProcessingAttachments(ProcessingRecordDetail detail)
        {
            IReadOnlyList<AttachmentDraft> attachments = detail?.Attachments ?? Array.Empty<AttachmentDraft>();
            if (attachments.Count == 0 && !string.IsNullOrWhiteSpace(detail?.RecordCode))
            {
                attachments = _dataService.GetRecordForm(detail.RecordCode)?.Attachments ?? attachments;
                detail.Attachments = attachments;
            }

            ReplaceItems(Attachments, attachments);
            OnPropertyChanged(nameof(HasAttachments));
        }

        private bool ShouldOfferInitialResultDocuments()
        {
            return GetProcessStepNumber(ProcessingStatus) == 5
                && !HasAllInitialResultDocuments();
        }

        private bool HasAllInitialResultDocuments()
        {
            return InitialResultDocumentGenerator.HasDocument(Attachments, "phieu_de_xuat.docx")
                && InitialResultDocumentGenerator.HasDocument(Attachments, "phieu_huong_dan.docx")
                && InitialResultDocumentGenerator.HasDocument(Attachments, "thong_bao.docx");
        }

        private static IReadOnlyList<string> GetAllowedProcessingStatuses()
        {
            var statuses = new[]
            {
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Kết quả xử lý ban đầu",
                "Chuyển cơ quan khác",
                "Chờ kết quả",
                "Đã giải quyết"
            };

            return AuthContext.IsOfficer
                ? statuses.Where(CanSelectProcessingStatus).ToList()
                : statuses;
        }

        private static IReadOnlyList<ProcessStep> NormalizeProcessSteps(IEnumerable<ProcessStep> source)
        {
            var sourceItems = (source ?? Array.Empty<ProcessStep>()).ToList();
            var hadLeadershipStep = sourceItems.Any(item => string.Equals(item?.Title, "Lãnh đạo duyệt", StringComparison.Ordinal));
            var result = sourceItems
                .Where(item => item != null && !string.Equals(item.Title, "Lãnh đạo duyệt", StringComparison.Ordinal))
                .ToList();

            for (var index = 0; index < result.Count; index++)
            {
                var step = result[index];
                step.StepNumber = index + 1;
                if (hadLeadershipStep && string.Equals(step.Title, "Kết thúc", StringComparison.Ordinal))
                {
                    step.Title = "Lưu hồ sơ";
                }
                step.HasPreviousStep = index > 0;
                step.HasNextStep = index < result.Count - 1;
                step.IsPreviousConnectorDone = index > 0 && (step.IsDone || step.IsCurrent);
                step.IsNextConnectorDone = index < result.Count - 1 && step.IsDone;
            }

            return result;
        }

        private static IReadOnlyList<ProcessHistoryItem> NormalizeProcessHistory(IEnumerable<ProcessHistoryItem> source)
        {
            var result = (source ?? Array.Empty<ProcessHistoryItem>())
                .Where(item => item != null && !string.Equals(item.Title, "Lãnh đạo duyệt", StringComparison.Ordinal))
                .ToList();
            for (var index = 0; index < result.Count; index++)
            {
                result[index].HasNextItem = index < result.Count - 1;
            }
            return result;
        }

        private static bool CanSelectProcessingStatus(string status)
        {
            return !AuthContext.IsOfficer || GetProcessStepNumber(status) >= AssignedProcessStep;
        }

        private static int GetProcessStepNumber(string status)
        {
            return status switch
            {
                "Mới tiếp nhận" => 1,
                "Đang phân loại" => 2,
                "Đã phân công" => 3,
                "Đang xác minh" => 4,
                "Đang chờ bổ sung tài liệu" => 5,
                "Chờ bổ sung tài liệu" => 5,
                "Kết quả xử lý ban đầu" => 5,
                "Chuyển cơ quan khác" => 6,
                "Chờ kết quả" => 7,
                "Đã giải quyết" => 8,
                _ => 4
            };
        }

        private void RaisePageCommandStates()
        {
            _previousPageCommand.RaiseCanExecuteChanged();
            _nextPageCommand.RaiseCanExecuteChanged();
        }
    }
}
