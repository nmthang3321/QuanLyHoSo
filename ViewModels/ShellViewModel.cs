using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private DashboardViewModel _dashboardViewModel;
        private RecordInputViewModel _recordInputViewModel;
        private RecordListViewModel _recordListViewModel;
        private RecordProcessingViewModel _recordProcessingViewModel;
        private StaffTrackingViewModel _staffTrackingViewModel;
        private SettingsViewModel _settingsViewModel;

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;
        private AppUser _currentUser;

        public ShellViewModel()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                AppDataService.Instance.Initialize();
                LogElapsed("InitializeDatabase", stopwatch);
            }
            catch (LanServerUnavailableException ex)
            {
                AppLogger.Error("Shell", "InitializeDatabase", ex, "Cannot connect to admin LAN server.");
                MessageBox.Show(ex.Message, "Không kết nối được máy server", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                CreateNavigationItem("Dashboard", "Tổng quan", "\uE80F"),
                CreateNavigationItem("Input", "Nhập dữ liệu", "\uF7DD", "pack://application:,,,/Assets/Fonts/#Material Symbols Outlined"),
                CreateNavigationItem("RecordList", "Danh sách hồ sơ", "\uE8FD"),
                CreateNavigationItem("Processing", "Phân loại & Xử lý", "\uE72C", "pack://application:,,,/Assets/Fonts/#Material Symbols Outlined"),
                CreateNavigationItem("StaffTracking", "Theo dõi cán bộ", "\uE716")
            };
            SettingsNavigationItem = CreateNavigationItem("Settings", "Cài đặt", "\uE713");

            SignOutCommand = new RelayCommand(SignOut);
            CurrentViewModel = new LoginViewModel(SignIn);
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public NavigationItem SettingsNavigationItem { get; }
        public ICommand SignOutCommand { get; }
        public bool IsAuthenticated => _currentUser != null;
        public string CurrentUserDisplayName => _currentUser?.DisplayName ?? string.Empty;
        public string CurrentUserRoleText => _currentUser?.RoleText ?? string.Empty;

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

        private NavigationItem CreateNavigationItem(string key, string title, string iconGlyph, string iconFontFamily = "Segoe MDL2 Assets")
        {
            return new NavigationItem
            {
                Key = key,
                Title = title,
                IconGlyph = iconGlyph,
                IconFontFamily = iconFontFamily,
                Command = new RelayCommand(() => NavigateTo(key), () => CanNavigateTo(key))
            };
        }

        private void NavigateTo(string key, string selectedNavigationKey = null)
        {
            if (!IsAuthenticated || !CanNavigateTo(key))
            {
                return;
            }

            if (CurrentPageKey == "Input" && !RecordInputViewModel.ConfirmLeaveWithoutSaving())
            {
                return;
            }

            if (key == "Input" && selectedNavigationKey == null)
            {
                RecordInputViewModel.PrepareNewRecord();
            }

            CurrentPageKey = key;
            CurrentViewModel = key switch
            {
                "Input" => RecordInputViewModel,
                "RecordList" => RecordListViewModel,
                "Processing" => RecordProcessingViewModel,
                "StaffTracking" => StaffTrackingViewModel,
                "Settings" => SettingsViewModel,
                _ => DashboardViewModel
            };

            UpdateNavigationSelection(selectedNavigationKey ?? key);
            RefreshCurrentPage(key);
        }

        private void UpdateNavigationSelection(string key)
        {
            foreach (var item in NavigationItems)
            {
                item.IsSelected = item.Key == key;
            }

            SettingsNavigationItem.IsSelected = key == "Settings";
        }

        private void RefreshCurrentPage(string key)
        {
            switch (key)
            {
                case "Dashboard":
                    _dashboardViewModel.Reload();
                    break;
                case "RecordList":
                    _recordListViewModel.Reload();
                    break;
                case "Processing":
                    _recordProcessingViewModel.Reload();
                    break;
            }
        }

        private void EditRecordFromList(string recordCode)
        {
            if (!AuthContext.CanWrite)
            {
                return;
            }

            RecordInputViewModel.LoadRecord(recordCode);
            NavigateTo("Input", selectedNavigationKey: "RecordList");
        }

        private void ClassifyRecordFromList(string recordCode)
        {
            RecordProcessingViewModel.OpenRecord(recordCode, returnToPreviousPage: true);
            NavigateTo("Processing", selectedNavigationKey: "RecordList");
        }

        private DashboardViewModel DashboardViewModel => _dashboardViewModel ??= new DashboardViewModel();

        private RecordInputViewModel RecordInputViewModel => _recordInputViewModel ??= new RecordInputViewModel(() => NavigateTo("RecordList"));

        private RecordListViewModel RecordListViewModel => _recordListViewModel ??= new RecordListViewModel(
            () => NavigateTo(AuthContext.CanCreateRecord ? "Input" : "Dashboard"),
            EditRecordFromList,
            ClassifyRecordFromList);

        private RecordProcessingViewModel RecordProcessingViewModel => _recordProcessingViewModel ??= new RecordProcessingViewModel(
            () => NavigateTo("RecordList"));

        private StaffTrackingViewModel StaffTrackingViewModel => _staffTrackingViewModel ??= new StaffTrackingViewModel();

        private SettingsViewModel SettingsViewModel => _settingsViewModel ??= new SettingsViewModel();

        private void SignIn(AppUser user)
        {
            AuthContext.SignIn(user);
            _currentUser = user;
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRoleText));
            UpdateNavigationVisibility();
            RaiseNavigationCommandStates();
            NavigateTo(AuthContext.IsOfficer ? "RecordList" : "Dashboard");
        }

        private void SignOut()
        {
            AuthContext.SignOut();
            _currentUser = null;
            _dashboardViewModel = null;
            _recordInputViewModel = null;
            _recordListViewModel = null;
            _recordProcessingViewModel = null;
            _staffTrackingViewModel = null;
            _settingsViewModel = null;
            CurrentPageKey = null;
            UpdateNavigationSelection(null);
            CurrentViewModel = new LoginViewModel(SignIn);
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRoleText));
            UpdateNavigationVisibility();
            RaiseNavigationCommandStates();
        }

        private static bool CanNavigateTo(string key)
        {
            return key != "Input" || AuthContext.CanWrite;
        }

        private void RaiseNavigationCommandStates()
        {
            foreach (var item in NavigationItems)
            {
                (item.Command as RelayCommand)?.RaiseCanExecuteChanged();
            }

            (SettingsNavigationItem.Command as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateNavigationVisibility()
        {
            foreach (var item in NavigationItems)
            {
                item.IsVisible = item.Key != "Input" || !AuthContext.IsOfficer;
            }
        }

        private static void LogElapsed(string action, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            AppLogger.Info("Shell", action, $"Completed in {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}
