using System.Diagnostics;
using System.Collections.ObjectModel;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly RecordInputViewModel _recordInputViewModel;
        private readonly RecordListViewModel _recordListViewModel;
        private readonly RecordProcessingViewModel _recordProcessingViewModel;
        private readonly ExportViewModel _exportViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;

        public ShellViewModel()
        {
            var stopwatch = Stopwatch.StartNew();
            AppDataService.Instance.Initialize();
            LogElapsed("InitializeDatabase", stopwatch);

            stopwatch.Restart();
            _dashboardViewModel = new DashboardViewModel();
            _recordInputViewModel = new RecordInputViewModel(() => NavigateTo("RecordList"));
            _recordListViewModel = new RecordListViewModel(() => NavigateTo("Input"), EditRecordFromList);
            _recordProcessingViewModel = new RecordProcessingViewModel();
            _exportViewModel = new ExportViewModel();
            _settingsViewModel = new SettingsViewModel();
            LogElapsed("CreatePageViewModels", stopwatch);

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                CreateNavigationItem("Dashboard", "Tổng quan", "\uE80F"),
                CreateNavigationItem("Input", "Nhập dữ liệu", "\uE8A5"),
                CreateNavigationItem("Processing", "Phân loại & Xử lý", "\uE8F9"),
                CreateNavigationItem("Export", "Xuất dữ liệu", "\uE896")
            };
            SettingsNavigationItem = CreateNavigationItem("Settings", "Cài đặt", "\uE713");

            NavigateTo("Dashboard");
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public NavigationItem SettingsNavigationItem { get; }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentPageKey
        {
            get => _currentPageKey;
            private set => SetProperty(ref _currentPageKey, value);
        }

        private NavigationItem CreateNavigationItem(string key, string title, string iconGlyph)
        {
            return new NavigationItem
            {
                Key = key,
                Title = title,
                IconGlyph = iconGlyph,
                Command = new RelayCommand(() => NavigateTo(key))
            };
        }

        private void NavigateTo(string key)
        {
            if (key == "RecordList")
            {
                _recordListViewModel.Reload();
            }

            CurrentPageKey = key;
            CurrentViewModel = key switch
            {
                "Input" => _recordInputViewModel,
                "RecordList" => _recordListViewModel,
                "Processing" => _recordProcessingViewModel,
                "Export" => _exportViewModel,
                "Settings" => _settingsViewModel,
                _ => _dashboardViewModel
            };

            foreach (var item in NavigationItems)
            {
                item.IsSelected = item.Key == key || (key == "RecordList" && item.Key == "Input");
            }

            SettingsNavigationItem.IsSelected = key == "Settings";
        }

        private void EditRecordFromList(string recordCode)
        {
            _recordInputViewModel.LoadRecord(recordCode);
            NavigateTo("Input");
        }

        private static void LogElapsed(string action, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            AppLogger.Info("Shell", action, $"Completed in {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}
