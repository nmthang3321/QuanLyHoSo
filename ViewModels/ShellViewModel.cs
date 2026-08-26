using System.Collections.ObjectModel;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly RecordInputViewModel _recordInputViewModel;
        private readonly RecordProcessingViewModel _recordProcessingViewModel;
        private readonly ExportViewModel _exportViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;

        public ShellViewModel()
        {
            AppDataService.Instance.Initialize();

            _dashboardViewModel = new DashboardViewModel();
            _recordInputViewModel = new RecordInputViewModel();
            _recordProcessingViewModel = new RecordProcessingViewModel();
            _exportViewModel = new ExportViewModel();
            _settingsViewModel = new SettingsViewModel();

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                CreateNavigationItem("Dashboard", "Tổng quan", "\uE80F"),
                CreateNavigationItem("Input", "Nhập dữ liệu", "\uE8A5"),
                CreateNavigationItem("Processing", "Phân loại & Xử lý", "\uE8F9"),
                CreateNavigationItem("Export", "Xuất dữ liệu", "\uE896"),
                CreateNavigationItem("Settings", "Cài đặt", "\uE713")
            };

            NavigateTo("Dashboard");
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

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
            CurrentPageKey = key;
            CurrentViewModel = key switch
            {
                "Input" => _recordInputViewModel,
                "Processing" => _recordProcessingViewModel,
                "Export" => _exportViewModel,
                "Settings" => _settingsViewModel,
                _ => _dashboardViewModel
            };

            foreach (var item in NavigationItems)
            {
                item.IsSelected = item.Key == key;
            }
        }
    }
}
