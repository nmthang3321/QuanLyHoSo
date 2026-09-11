using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
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
        private SettingsGuideViewModel _settingsGuideViewModel;
        private readonly DispatcherTimer _notificationBadgeRefreshTimer;

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;
        private AppUser _currentUser;
        private bool _isRefreshingNotificationBadge;

        public ShellViewModel()
        {
            var stopwatch = Stopwatch.StartNew();
            if (!AppPathSettings.Current.IsClientMode)
            {
                AppDataService.Instance.Initialize();
                LogElapsed("InitializeDatabase", stopwatch);
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

            _notificationBadgeRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _notificationBadgeRefreshTimer.Tick += async (_, _) => await RefreshStaffNotificationBadgeAsync();

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
                Command = new RelayCommand(() => NavigateTo(key, resetPage: true), () => CanNavigateTo(key))
            };
        }

        private void NavigateTo(string key, string selectedNavigationKey = null, bool resetPage = false)
        {
            if (!IsAuthenticated || !CanNavigateTo(key))
            {
                return;
            }

            if (CurrentPageKey == "Input" && !RecordInputViewModel.ConfirmLeaveWithoutSaving())
            {
                return;
            }

            if (resetPage)
            {
                ResetPageViewModel(key);
            }

            if (key == "Input" && selectedNavigationKey == null)
            {
                RecordInputViewModel.PrepareNewRecord();
            }

            if (key == "Processing" && selectedNavigationKey == null)
            {
                RecordProcessingViewModel.PrepareQueue();
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

        private void ResetPageViewModel(string key)
        {
            switch (key)
            {
                case "Dashboard":
                    _dashboardViewModel = null;
                    break;
                case "Input":
                    _recordInputViewModel = null;
                    break;
                case "RecordList":
                    _recordListViewModel = null;
                    break;
                case "Processing":
                    _recordProcessingViewModel = null;
                    break;
                case "StaffTracking":
                    _staffTrackingViewModel = null;
                    break;
                case "Settings":
                    _settingsViewModel = null;
                    _settingsGuideViewModel = null;
                    break;
            }
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
                case "StaffTracking":
                    _staffTrackingViewModel.Reload();
                    UpdateStaffNotificationBadge();
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

        private StaffTrackingViewModel StaffTrackingViewModel => _staffTrackingViewModel ??= new StaffTrackingViewModel(SetStaffNotificationBadge);

        private SettingsViewModel SettingsViewModel => _settingsViewModel ??= new SettingsViewModel(OpenSettingsGuide);

        private SettingsGuideViewModel SettingsGuideViewModel => _settingsGuideViewModel ??= new SettingsGuideViewModel(
            () => NavigateTo("Settings"));

        private void OpenSettingsGuide()
        {
            if (!IsAuthenticated)
            {
                return;
            }

            CurrentPageKey = "SettingsGuide";
            CurrentViewModel = SettingsGuideViewModel;
            UpdateNavigationSelection("Settings");
        }

        private void SignIn(AppUser user)
        {
            AuthContext.SignIn(user);
            if (user.MustChangePassword)
            {
                CurrentPageKey = "RequiredPasswordChange";
                CurrentViewModel = new RequiredPasswordChangeViewModel(user, CompleteSignIn, SignOut);
                UpdateNavigationSelection(null);
                return;
            }

            CompleteSignIn(user);
        }

        private void CompleteSignIn(AppUser user)
        {
            AuthContext.SignIn(user);
            _currentUser = user;
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRoleText));
            UpdateNavigationVisibility();
            RaiseNavigationCommandStates();
            UpdateStaffNotificationBadge();
            _notificationBadgeRefreshTimer.Start();
            NavigateTo(AuthContext.IsOfficer ? "RecordList" : "Dashboard");
        }

        private void SignOut()
        {
            _notificationBadgeRefreshTimer.Stop();
            AuthContext.SignOut();
            _currentUser = null;
            _dashboardViewModel = null;
            _recordInputViewModel = null;
            _recordListViewModel = null;
            _recordProcessingViewModel = null;
            _staffTrackingViewModel = null;
            _settingsViewModel = null;
            _settingsGuideViewModel = null;
            CurrentPageKey = null;
            UpdateNavigationSelection(null);
            CurrentViewModel = new LoginViewModel(SignIn);
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRoleText));
            UpdateNavigationVisibility();
            RaiseNavigationCommandStates();
        }

        private void UpdateStaffNotificationBadge()
        {
            var unreadCount = AuthContext.IsOfficer || AuthContext.IsLeader || AuthContext.IsAdmin
                ? AppDataService.Instance.CountUnreadLeadershipNotices(
                    AuthContext.CurrentDisplayName,
                    AuthContext.IsLeader,
                    AuthContext.IsAdmin)
                : 0;
            SetStaffNotificationBadge(unreadCount);
        }

        private async Task RefreshStaffNotificationBadgeAsync()
        {
            if (_isRefreshingNotificationBadge || !IsAuthenticated)
            {
                return;
            }

            _isRefreshingNotificationBadge = true;
            var signedInUser = _currentUser;
            var officerName = AuthContext.CurrentDisplayName;
            var adminOnly = AuthContext.IsLeader;
            var includeAll = AuthContext.IsAdmin;

            try
            {
                var unreadCount = await Task.Run(() => AppDataService.Instance.CountUnreadLeadershipNotices(
                    officerName,
                    adminOnly,
                    includeAll));

                if (ReferenceEquals(_currentUser, signedInUser))
                {
                    SetStaffNotificationBadge(unreadCount);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Shell", "RefreshNotificationBadge", ex, "Could not refresh unread notification badge.");
            }
            finally
            {
                _isRefreshingNotificationBadge = false;
            }
        }

        private void SetStaffNotificationBadge(int unreadCount)
        {
            foreach (var item in NavigationItems)
            {
                if (item.Key == "StaffTracking")
                {
                    item.BadgeCount = unreadCount;
                    return;
                }
            }
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
                item.IsVisible = item.Key != "Input" || AuthContext.CanCreateRecord;
            }
        }

        private static void LogElapsed(string action, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            AppLogger.Info("Shell", action, $"Completed in {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}
