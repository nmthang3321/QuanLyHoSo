using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel = new DashboardViewModel();
        private readonly RecordInputViewModel _recordInputViewModel = new RecordInputViewModel();
        private readonly RecordProcessingViewModel _recordProcessingViewModel = new RecordProcessingViewModel();
        private readonly ExportViewModel _exportViewModel = new ExportViewModel();
        private readonly SettingsViewModel _settingsViewModel = new SettingsViewModel();

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;

        public ShellViewModel()
        {
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

