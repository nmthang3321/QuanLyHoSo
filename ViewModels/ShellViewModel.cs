using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Input;
using QuanLyHoSo.ApplicationServices.Abstractions;
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
        private readonly IApplicationDataService _dataService;

        private ViewModelBase _currentViewModel;
        private string _currentPageKey;
        private AppUser _currentUser;
        private bool _isRefreshingNotificationBadge;

        public ShellViewModel()
            : this(AppDataService.Instance)
        {
        }

        public ShellViewModel(IApplicationDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            var stopwatch = Stopwatch.StartNew();
            if (!AppPathSettings.Current.IsClientMode)
            {
                _dataService.Initialize();
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
            _notificationBadgeRefreshTimer.Tick += NotificationBadgeRefreshTimer_Tick;

            SignOutCommand = new RelayCommand(SignOut);
            CurrentViewModel = new LoginViewModel(_dataService, SignIn);
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
                    DisposeAndClear(ref _dashboardViewModel);
                    break;
                case "Input":
                    DisposeAndClear(ref _recordInputViewModel);
                    break;
                case "RecordList":
                    DisposeAndClear(ref _recordListViewModel);
                    break;
                case "Processing":
                    DisposeAndClear(ref _recordProcessingViewModel);
                    break;
                case "StaffTracking":
                    DisposeAndClear(ref _staffTrackingViewModel);
                    break;
                case "Settings":
                    DisposeAndClear(ref _settingsViewModel);
                    DisposeAndClear(ref _settingsGuideViewModel);
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

        private DashboardViewModel DashboardViewModel => _dashboardViewModel ??= new DashboardViewModel(_dataService);

        private RecordInputViewModel RecordInputViewModel => _recordInputViewModel ??= new RecordInputViewModel(_dataService, () => NavigateTo("RecordList"));

        private RecordListViewModel RecordListViewModel => _recordListViewModel ??= new RecordListViewModel(
            _dataService,
            () => NavigateTo(AuthContext.CanCreateRecord ? "Input" : "Dashboard"),
            EditRecordFromList,
            ClassifyRecordFromList);

        private RecordProcessingViewModel RecordProcessingViewModel => _recordProcessingViewModel ??= new RecordProcessingViewModel(
            _dataService,
            () => NavigateTo("RecordList"));

        private StaffTrackingViewModel StaffTrackingViewModel => _staffTrackingViewModel ??= new StaffTrackingViewModel(_dataService, SetStaffNotificationBadge);

        private SettingsViewModel SettingsViewModel => _settingsViewModel ??= new SettingsViewModel(_dataService, OpenSettingsGuide);

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
                CurrentViewModel = new RequiredPasswordChangeViewModel(_dataService, user, CompleteSignIn, SignOut);
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
            DisposePageViewModels();
            CurrentPageKey = null;
            UpdateNavigationSelection(null);
            CurrentViewModel = new LoginViewModel(_dataService, SignIn);
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRoleText));
            UpdateNavigationVisibility();
            RaiseNavigationCommandStates();
        }

        private void UpdateStaffNotificationBadge()
        {
            var unreadCount = AuthContext.IsOfficer || AuthContext.IsLeader || AuthContext.IsAdmin
                ? _dataService.CountUnreadLeadershipNotices(
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
                var unreadCount = await Task.Run(() => _dataService.CountUnreadLeadershipNotices(
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

        private async void NotificationBadgeRefreshTimer_Tick(object sender, EventArgs e)
        {
            await RefreshStaffNotificationBadgeAsync();
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

        private void DisposePageViewModels()
        {
            DisposeAndClear(ref _dashboardViewModel);
            DisposeAndClear(ref _recordInputViewModel);
            DisposeAndClear(ref _recordListViewModel);
            DisposeAndClear(ref _recordProcessingViewModel);
            DisposeAndClear(ref _staffTrackingViewModel);
            DisposeAndClear(ref _settingsViewModel);
            DisposeAndClear(ref _settingsGuideViewModel);
        }

        private static void DisposeAndClear<TViewModel>(ref TViewModel viewModel)
            where TViewModel : ViewModelBase
        {
            viewModel?.Dispose();
            viewModel = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notificationBadgeRefreshTimer.Stop();
                _notificationBadgeRefreshTimer.Tick -= NotificationBadgeRefreshTimer_Tick;
                DisposePageViewModels();
            }

            base.Dispose(disposing);
        }
    }
}
