using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordProcessingViewModel : ViewModelBase
    {
        private const int DefaultPageSize = 20;
        private const int MinimumPageSize = 1;
        private const int MaximumPageSize = 50;

        private readonly AppDataService _dataService;
        private readonly Action _goBackToPreviousPage;
        private readonly RelayCommand _nextPageCommand;
        private readonly RelayCommand _previousPageCommand;
        private int _currentPage = 1;
        private int _pageSize = DefaultPageSize;
        private string _pageSizeText = DefaultPageSize.ToString(CultureInfo.InvariantCulture);
        private string _searchText;
        private RecordFormDraft _selectedRecordDetail;
        private ProcessingRecordDetail _selectedProcessingDetail;
        private DateTime? _selectedProcessingDate;
        private string _processingContent;
        private string _processingNote;
        private string _processingProcessorName;
        private string _processingStatus;
        private string _selectedArea;
        private string _selectedMetricKey = "All";
        private string _selectedPriority;
        private string _selectedStatus;
        private bool _shouldReturnToPreviousPage;
        private int _totalPages = 1;
        private string _totalRecordsText;

        public RecordProcessingViewModel(Action goBackToPreviousPage = null)
        {
            _dataService = AppDataService.Instance;
            _goBackToPreviousPage = goBackToPreviousPage ?? (() => { });
            Metrics = new ObservableCollection<DashboardMetric>();
            QueueRecords = new ObservableCollection<ProcessingQueueRecord>();
            ProcessSteps = new ObservableCollection<ProcessStep>();
            History = new ObservableCollection<ProcessHistoryItem>();
            ProcessorNames = new ObservableCollection<string>(_dataService.GetProcessorNames());
            ProcessingStatuses = new ObservableCollection<string>
            {
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Đang chờ bổ sung tài liệu",
                "Chờ kết quả",
                "Đã giải quyết",
                "Chuyển cơ quan khác"
            };
            StatusFilters = new ObservableCollection<string>
            {
                "Tất cả",
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Chờ kết quả",
                "Đang chờ bổ sung tài liệu",
                "Chuyển cơ quan khác"
            };
            AreaFilters = new ObservableCollection<string>(_dataService.GetAreaNames(includeAll: true));
            PriorityFilters = new ObservableCollection<string>(_dataService.GetCatalogValues("Priority", includeAll: true));
            ApplyFilterCommand = new RelayCommand(Reload);
            ViewRecordCommand = new RelayCommand(ViewRecord);
            ViewProcessingDetailCommand = new RelayCommand(ViewProcessingDetail);
            OpenProcessingDetailCommand = new RelayCommand(OpenProcessingDetail);
            SelectMetricCommand = new RelayCommand(SelectMetric);
            _previousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            _nextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            CloseDetailCommand = new RelayCommand(CloseDetail);
            BackToQueueCommand = new RelayCommand(BackToQueue);
            SaveProcessingCommand = new RelayCommand(SaveProcessing, () => CanUpdateProcessing);

            _selectedStatus = StatusFilters[0];
            _selectedArea = AreaFilters.Count > 0 ? AreaFilters[0] : "Tất cả";
            _selectedPriority = PriorityFilters.Count > 0 ? PriorityFilters[0] : "Tất cả";

            Reload();
        }

        public ObservableCollection<DashboardMetric> Metrics { get; }
        public ObservableCollection<ProcessingQueueRecord> QueueRecords { get; }
        public ObservableCollection<ProcessStep> ProcessSteps { get; }
        public ObservableCollection<ProcessHistoryItem> History { get; }
        public ObservableCollection<string> ProcessorNames { get; }
        public ObservableCollection<string> ProcessingStatuses { get; }
        public ObservableCollection<string> StatusFilters { get; }
        public ObservableCollection<string> AreaFilters { get; }
        public ObservableCollection<string> PriorityFilters { get; }
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
        public bool CanUpdateProcessing => SelectedProcessingDetail != null && AuthContext.CanEditRecord(SelectedProcessingDetail.ProcessorName);

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
            set => SetProperty(ref _searchText, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    Reload();
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
                    Reload();
                }
            }
        }

        public string SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (SetProperty(ref _selectedPriority, value))
                {
                    Reload();
                }
            }
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
                }
            }
        }

        public bool IsProcessingDetailOpen => SelectedProcessingDetail != null;

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

        public string ProcessingNote
        {
            get => _processingNote;
            set => SetProperty(ref _processingNote, value);
        }

        public void Reload()
        {
            var metrics = _dataService.GetProcessingQueueMetrics();
            UpdateMetricSelection(metrics);
            ReplaceItems(Metrics, metrics);

            var totalRecords = _dataService.CountProcessingQueueRecords(
                SearchText,
                SelectedStatus,
                SelectedArea,
                SelectedPriority,
                _selectedMetricKey);
            TotalRecordsText = $"{totalRecords:N0} hồ sơ phù hợp";
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

        private void LoadPage()
        {
            var skip = (CurrentPage - 1) * _pageSize;
            ReplaceItems(QueueRecords, _dataService.GetProcessingQueueRecords(
                SearchText,
                SelectedStatus,
                SelectedArea,
                SelectedPriority,
                _selectedMetricKey,
                skip,
                _pageSize));
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
            _selectedArea = AreaFilters.Count > 0 ? AreaFilters[0] : "Tất cả";
            _selectedPriority = PriorityFilters.Count > 0 ? PriorityFilters[0] : "Tất cả";
            _searchText = string.Empty;
            OnPropertyChanged(nameof(SelectedStatus));
            OnPropertyChanged(nameof(SelectedArea));
            OnPropertyChanged(nameof(SelectedPriority));
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

        public void OpenRecord(string recordCode, bool returnToPreviousPage = false)
        {
            _shouldReturnToPreviousPage = returnToPreviousPage;
            OpenProcessingDetail(recordCode);
        }

        private void OpenProcessingDetail(string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return;
            }

            SelectedProcessingDetail = _dataService.GetProcessingRecordDetail(recordCode);
            ReplaceItems(ProcessorNames, _dataService.GetProcessorNames());
            ReplaceItems(ProcessSteps, SelectedProcessingDetail.Steps);
            ReplaceItems(History, SelectedProcessingDetail.History);
            ProcessingStatus = SelectedProcessingDetail.Status;
            SelectedProcessingDate = ParseProcessingDate(SelectedProcessingDetail.ProcessingDate) ?? DateTime.Now;
            ProcessingProcessorName = SelectedProcessingDetail.ProcessorName;
            ProcessingContent = SelectedProcessingDetail.ProcessContent;
            ProcessingNote = SelectedProcessingDetail.ProcessNote;
        }

        private void CloseDetail()
        {
            SelectedRecordDetail = null;
        }

        private void BackToQueue()
        {
            SelectedProcessingDetail = null;
            ProcessSteps.Clear();
            History.Clear();
            if (_shouldReturnToPreviousPage)
            {
                _shouldReturnToPreviousPage = false;
                _goBackToPreviousPage();
                return;
            }

            Reload();
        }

        private void SaveProcessing()
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

            if (missingFields.Count > 0)
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin bắt buộc:\n\n- " + string.Join("\n- ", missingFields), "Thiếu thông tin xử lý", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var recordCode = SelectedProcessingDetail.RecordCode;
            try
            {
                _dataService.UpdateProcessingRecord(
                    recordCode,
                    ProcessingStatus,
                    SelectedProcessingDate.Value,
                    ProcessingProcessorName,
                    ProcessingContent,
                    ProcessingNote);

                AppLogger.Info("Processing", "UpdateProcessingRecord", "Processing record updated.", recordCode);
                SelectedProcessingDetail = _dataService.GetProcessingRecordDetail(recordCode);
                ReplaceItems(ProcessSteps, SelectedProcessingDetail.Steps);
                ReplaceItems(History, SelectedProcessingDetail.History);
                ReplaceItems(ProcessorNames, _dataService.GetProcessorNames());
                ProcessingStatus = SelectedProcessingDetail.Status;
                ProcessingProcessorName = SelectedProcessingDetail.ProcessorName;
                MessageBox.Show("Đã cập nhật xử lý hồ sơ.", "Cập nhật xử lý", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Processing", "UpdateProcessingRecord", ex, "Failed to update processing record.", recordCode);
                MessageBox.Show($"Không thể cập nhật xử lý hồ sơ.\n\nChi tiết: {ex.Message}", "Lỗi cập nhật xử lý", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void RaisePageCommandStates()
        {
            _previousPageCommand.RaiseCanExecuteChanged();
            _nextPageCommand.RaiseCanExecuteChanged();
        }
    }
}
