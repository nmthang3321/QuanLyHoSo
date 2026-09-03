using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using Forms = System.Windows.Forms;

namespace QuanLyHoSo.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private const string GitHubLatestReleaseApiUrl = "https://api.github.com/repos/nmthang3321/QuanLyHoSo/releases/latest";
        private const string GitHubReleasesApiUrl = "https://api.github.com/repos/nmthang3321/QuanLyHoSo/releases";
        private const string GitHubReleasesPageUrl = "https://github.com/nmthang3321/QuanLyHoSo/releases/latest";
        private const int CatalogDialogPageSize = 6;

        private readonly AppDataService _dataService;
        private readonly List<CatalogValueSetting> _allCatalogValues;
        private CatalogGroupSetting _selectedCatalogGroup;
        private CatalogValueSetting _selectedCatalogValue;
        private string _catalogValueText;
        private string _catalogSearchText;
        private string _selectedCatalogStatusFilter;
        private string _backupFolder;
        private string _backupStatus;
        private string _updateStatus;
        private string _lastBackupText;
        private string _latestReleaseUrl;
        private string _latestReleaseDownloadUrl;
        private string _latestReleaseVersion;
        private bool _isCheckingUpdate;
        private bool _hasAvailableUpdate;
        private bool _isCatalogDialogOpen;
        private bool _isSystemLogDialogOpen;
        private bool _isGeneralSettingsDialogOpen;
        private bool _isGuideDialogOpen;
        private bool _isUserManagementDialogOpen;
        private string _databasePathText;
        private string _logFolderText;
        private string _generalSettingsStatus;
        private string _selectedDataAccessMode;
        private string _adminMachineNameText;
        private string _adminServerUrlText;
        private AppUser _selectedUser;
        private string _userNameText;
        private string _userDisplayNameText;
        private string _selectedUserRole;
        private string _userPasswordText;
        private bool _isUserActive = true;
        private int _catalogCurrentPage = 1;
        private int _catalogFilteredCount;

        public SettingsViewModel()
        {
            _dataService = AppDataService.Instance;

            CatalogGroups = new ObservableCollection<CatalogGroupSetting>
            {
                CreateCatalogGroup("ReceiveSource", "Nguồn tiếp nhận", "Dùng trong form nhập hồ sơ", "\uE8A5", "#0B5CFF", "#EEF4FF"),
                CreateCatalogGroup("CaseType", "Loại vụ việc", "Phân loại bản chất hồ sơ", "\uE9F9", "#1FA24A", "#EAF8F0"),
                CreateCatalogGroup("Field", "Lĩnh vực", "Lọc, thống kê và xuất dữ liệu", "\uE825", "#7147D8", "#F3EEFF"),
                CreateCatalogGroup("ContentGroup", "Nhóm nội dung", "Nhóm hóa nội dung phản ánh", "\uECA5", "#E85D04", "#FFF2E7"),
                CreateCatalogGroup("Priority", "Mức độ vụ việc", "Dùng cho mức độ nghiêm trọng của vụ việc", "\uE734", "#E43D5C", "#FFF0F3"),
                CreateCatalogGroup("ProcessorName", "Tên cán bộ xử lý", "Dùng khi cập nhật xử lý hồ sơ", "\uE77B", "#0B5CFF", "#EEF4FF"),
                CreateCatalogGroup("ExpectedHandlingMethod", "Hướng xử lý", "Định hướng xử lý dự kiến", "\uE774", "#00A6B2", "#E9FAFC")
            };

            CatalogValues = new ObservableCollection<CatalogValueSetting>();
            SystemLogs = new ObservableCollection<SystemLogEntry>();
            CatalogStatusFilters = new ObservableCollection<string>
            {
                "Tất cả trạng thái",
                "Đang sử dụng",
                "Ngưng sử dụng"
            };
            _allCatalogValues = new List<CatalogValueSetting>();
            SoftwareInfos = new ObservableCollection<SoftwareInfo>();
            Users = new ObservableCollection<AppUser>();
            UserRoles = new ObservableCollection<string> { Models.UserRoles.Admin, Models.UserRoles.Officer, Models.UserRoles.Leader };
            DataAccessModes = new ObservableCollection<string> { "AdminHost", "Client" };

            SelectCatalogGroupCommand = new RelayCommand(SelectCatalogGroup);
            OpenCatalogDialogCommand = new RelayCommand(OpenCatalogDialog);
            CloseCatalogDialogCommand = new RelayCommand(() => IsCatalogDialogOpen = false);
            SelectCatalogValueCommand = new RelayCommand(SelectCatalogValue, value => value is CatalogValueSetting);
            DeleteCatalogValueForRowCommand = new RelayCommand(DeleteCatalogValueForRow, value => value is CatalogValueSetting);
            PreviousCatalogPageCommand = new RelayCommand(PreviousCatalogPage, () => CatalogCurrentPage > 1);
            NextCatalogPageCommand = new RelayCommand(NextCatalogPage, () => CatalogCurrentPage < CatalogTotalPages);
            OpenSystemLogDialogCommand = new RelayCommand(OpenSystemLogDialog);
            CloseSystemLogDialogCommand = new RelayCommand(() => IsSystemLogDialogOpen = false);
            RefreshSystemLogsCommand = new RelayCommand(RefreshSystemLogs);
            OpenGeneralSettingsDialogCommand = new RelayCommand(OpenGeneralSettingsDialog);
            CloseGeneralSettingsDialogCommand = new RelayCommand(() => IsGeneralSettingsDialogOpen = false);
            OpenGuideDialogCommand = new RelayCommand(() => IsGuideDialogOpen = true);
            CloseGuideDialogCommand = new RelayCommand(() => IsGuideDialogOpen = false);
            OpenUserManagementDialogCommand = new RelayCommand(OpenUserManagementDialog, () => AuthContext.CanManageUsers);
            CloseUserManagementDialogCommand = new RelayCommand(() => IsUserManagementDialogOpen = false);
            EditUserCommand = new RelayCommand(EditUser, value => value is AppUser);
            SaveUserCommand = new RelayCommand(SaveUser);
            NewUserCommand = new RelayCommand(ClearUserForm);
            DeleteUserCommand = new RelayCommand(DeleteUser, value => value is AppUser user && user.Id != AuthContext.CurrentUser?.Id);
            ChooseDatabasePathCommand = new RelayCommand(ChooseDatabasePath);
            ChooseLogFolderCommand = new RelayCommand(ChooseLogFolder);
            SaveGeneralSettingsCommand = new RelayCommand(SaveGeneralSettings);
            ResetGeneralSettingsCommand = new RelayCommand(ResetGeneralSettings);
            SaveCatalogValueCommand = new RelayCommand(SaveCatalogValue);
            CancelCatalogEditCommand = new RelayCommand(CancelCatalogEdit, () => IsEditingCatalogValue);
            AddCatalogValueCommand = new RelayCommand(AddCatalogValue);
            UpdateCatalogValueCommand = new RelayCommand(UpdateCatalogValue, () => SelectedCatalogValue != null);
            DeleteCatalogValueCommand = new RelayCommand(DeleteCatalogValue, () => SelectedCatalogValue != null);
            ChooseBackupFolderCommand = new RelayCommand(ChooseBackupFolder);
            BackupNowCommand = new RelayCommand(async () => await BackupNowAsync());
            RestoreDataCommand = new RelayCommand(async () => await RestoreDataAsync());
            CheckUpdateCommand = new RelayCommand(async () => await CheckUpdateAsync(), () => !IsCheckingUpdate);
            UpdateSoftwareCommand = new RelayCommand(async () => await UpdateSoftwareAsync(), () => HasAvailableUpdate && !IsCheckingUpdate);

            BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Backup");
            CatalogSearchText = string.Empty;
            SelectedCatalogStatusFilter = CatalogStatusFilters.Skip(1).FirstOrDefault() ?? CatalogStatusFilters.FirstOrDefault();
            CatalogCurrentPage = 1;
            DatabasePathText = _dataService.DatabasePath;
            LogFolderText = AppLogger.LogFolder;
            SelectedDataAccessMode = AppPathSettings.Current.DataAccessMode;
            AdminMachineNameText = AppPathSettings.Current.AdminMachineName;
            AdminServerUrlText = AppPathSettings.Current.AdminServerUrl;
            GeneralSettingsStatus = "Cấu hình DB, log và URL API được quản lý ở ứng dụng server.";
            LastBackupText = "Chưa có bản sao lưu trong phiên này";
            BackupStatus = "Sẵn sàng sao lưu dữ liệu";
            UpdateStatus = "Chưa kiểm tra cập nhật";
            _latestReleaseUrl = GitHubReleasesPageUrl;
            SelectedUserRole = UserRoles[1];

            RefreshSoftwareInfos();
            RefreshCatalogGroupCounts();
        }

        public ObservableCollection<CatalogGroupSetting> CatalogGroups { get; }
        public ObservableCollection<CatalogValueSetting> CatalogValues { get; }
        public ObservableCollection<SystemLogEntry> SystemLogs { get; }
        public ObservableCollection<string> CatalogStatusFilters { get; }
        public ObservableCollection<SoftwareInfo> SoftwareInfos { get; }
        public ObservableCollection<AppUser> Users { get; }
        public ObservableCollection<string> UserRoles { get; }
        public ObservableCollection<string> DataAccessModes { get; }

        public ICommand SelectCatalogGroupCommand { get; }
        public ICommand OpenCatalogDialogCommand { get; }
        public ICommand CloseCatalogDialogCommand { get; }
        public ICommand SelectCatalogValueCommand { get; }
        public ICommand DeleteCatalogValueForRowCommand { get; }
        public ICommand PreviousCatalogPageCommand { get; }
        public ICommand NextCatalogPageCommand { get; }
        public ICommand OpenSystemLogDialogCommand { get; }
        public ICommand CloseSystemLogDialogCommand { get; }
        public ICommand RefreshSystemLogsCommand { get; }
        public ICommand OpenGeneralSettingsDialogCommand { get; }
        public ICommand CloseGeneralSettingsDialogCommand { get; }
        public ICommand OpenGuideDialogCommand { get; }
        public ICommand CloseGuideDialogCommand { get; }
        public ICommand OpenUserManagementDialogCommand { get; }
        public ICommand CloseUserManagementDialogCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand NewUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ChooseDatabasePathCommand { get; }
        public ICommand ChooseLogFolderCommand { get; }
        public ICommand SaveGeneralSettingsCommand { get; }
        public ICommand ResetGeneralSettingsCommand { get; }
        public ICommand SaveCatalogValueCommand { get; }
        public ICommand CancelCatalogEditCommand { get; }
        public ICommand AddCatalogValueCommand { get; }
        public ICommand UpdateCatalogValueCommand { get; }
        public ICommand DeleteCatalogValueCommand { get; }
        public ICommand ChooseBackupFolderCommand { get; }
        public ICommand BackupNowCommand { get; }
        public ICommand RestoreDataCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand UpdateSoftwareCommand { get; }

        public CatalogGroupSetting SelectedCatalogGroup
        {
            get => _selectedCatalogGroup;
            set
            {
                if (SetProperty(ref _selectedCatalogGroup, value))
                {
                    SelectCatalogGroup(value);
                }
            }
        }

        public CatalogValueSetting SelectedCatalogValue
        {
            get => _selectedCatalogValue;
            set
            {
                if (SetProperty(ref _selectedCatalogValue, value))
                {
                    CatalogValueText = value?.Name ?? string.Empty;
                    OnPropertyChanged(nameof(IsEditingCatalogValue));
                    OnPropertyChanged(nameof(CatalogSubmitButtonText));
                    RaiseCatalogCommandState();
                }
            }
        }

        public string CatalogValueText
        {
            get => _catalogValueText;
            set => SetProperty(ref _catalogValueText, value);
        }

        public string CatalogSearchText
        {
            get => _catalogSearchText;
            set
            {
                if (SetProperty(ref _catalogSearchText, value))
                {
                    CatalogCurrentPage = 1;
                    ApplyCatalogFilters();
                }
            }
        }

        public string SelectedCatalogStatusFilter
        {
            get => _selectedCatalogStatusFilter;
            set
            {
                if (SetProperty(ref _selectedCatalogStatusFilter, value))
                {
                    CatalogCurrentPage = 1;
                    ApplyCatalogFilters();
                }
            }
        }

        public bool IsCatalogDialogOpen
        {
            get => _isCatalogDialogOpen;
            set => SetProperty(ref _isCatalogDialogOpen, value);
        }

        public bool IsSystemLogDialogOpen
        {
            get => _isSystemLogDialogOpen;
            set => SetProperty(ref _isSystemLogDialogOpen, value);
        }

        public bool IsGeneralSettingsDialogOpen
        {
            get => _isGeneralSettingsDialogOpen;
            set => SetProperty(ref _isGeneralSettingsDialogOpen, value);
        }

        public bool IsGuideDialogOpen
        {
            get => _isGuideDialogOpen;
            set => SetProperty(ref _isGuideDialogOpen, value);
        }

        public bool IsUserManagementDialogOpen
        {
            get => _isUserManagementDialogOpen;
            set => SetProperty(ref _isUserManagementDialogOpen, value);
        }

        public bool CanManageUsers => AuthContext.CanManageUsers;

        public AppUser SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    LoadSelectedUser();
                    OnPropertyChanged(nameof(IsEditingUser));
                    OnPropertyChanged(nameof(UserSubmitButtonText));
                    (DeleteUserCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsEditingUser => SelectedUser != null;
        public string UserSubmitButtonText => IsEditingUser ? "Lưu thay đổi" : "Thêm tài khoản";

        public string UserNameText
        {
            get => _userNameText;
            set => SetProperty(ref _userNameText, value);
        }

        public string UserDisplayNameText
        {
            get => _userDisplayNameText;
            set => SetProperty(ref _userDisplayNameText, value);
        }

        public string SelectedUserRole
        {
            get => _selectedUserRole;
            set => SetProperty(ref _selectedUserRole, value);
        }

        public string UserPasswordText
        {
            get => _userPasswordText;
            set => SetProperty(ref _userPasswordText, value);
        }

        public bool IsUserActive
        {
            get => _isUserActive;
            set => SetProperty(ref _isUserActive, value);
        }

        public string DatabasePathText
        {
            get => _databasePathText;
            set => SetProperty(ref _databasePathText, value);
        }

        public string LogFolderText
        {
            get => _logFolderText;
            set => SetProperty(ref _logFolderText, value);
        }

        public string GeneralSettingsStatus
        {
            get => _generalSettingsStatus;
            set => SetProperty(ref _generalSettingsStatus, value);
        }

        public string SelectedDataAccessMode
        {
            get => _selectedDataAccessMode;
            set
            {
                if (SetProperty(ref _selectedDataAccessMode, value))
                {
                    OnPropertyChanged(nameof(DataAccessModeText));
                }
            }
        }

        public string DataAccessModeText => SelectedDataAccessMode == "Client"
            ? "Máy trạm"
            : "Máy admin giữ DB";

        public string AdminMachineNameText
        {
            get => _adminMachineNameText;
            set => SetProperty(ref _adminMachineNameText, value);
        }

        public string AdminServerUrlText
        {
            get => _adminServerUrlText;
            set => SetProperty(ref _adminServerUrlText, value);
        }

        public int CatalogCurrentPage
        {
            get => _catalogCurrentPage;
            set
            {
                if (SetProperty(ref _catalogCurrentPage, value))
                {
                    OnPropertyChanged(nameof(CatalogPageText));
                    OnPropertyChanged(nameof(CatalogRowsText));
                    RaiseCatalogPageCommandState();
                }
            }
        }

        public int CatalogTotalPages => Math.Max(1, (int)Math.Ceiling(_catalogFilteredCount / (double)CatalogDialogPageSize));

        public string CatalogPageText => $"{CatalogCurrentPage}/{CatalogTotalPages}";

        public string CatalogRowsText
        {
            get
            {
                if (_catalogFilteredCount == 0)
                {
                    return "Không có danh mục phù hợp";
                }

                return $"Tổng {_catalogFilteredCount} danh mục";
            }
        }

        public string CatalogDialogTitle => SelectedCatalogGroup == null
            ? "QUẢN LÝ DANH MỤC"
            : $"QUẢN LÝ {SelectedCatalogGroup.Title.ToUpperInvariant()}";

        public bool IsEditingCatalogValue => SelectedCatalogValue != null;

        public string CatalogSubmitButtonText => IsEditingCatalogValue ? "Lưu thay đổi" : "Thêm mới";

        public string BackupFolder
        {
            get => _backupFolder;
            set => SetProperty(ref _backupFolder, value);
        }

        public string BackupStatus
        {
            get => _backupStatus;
            set => SetProperty(ref _backupStatus, value);
        }

        public string LastBackupText
        {
            get => _lastBackupText;
            set => SetProperty(ref _lastBackupText, value);
        }

        public string UpdateStatus
        {
            get => _updateStatus;
            set => SetProperty(ref _updateStatus, value);
        }

        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set
            {
                if (SetProperty(ref _isCheckingUpdate, value))
                {
                    (CheckUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (UpdateSoftwareCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasAvailableUpdate
        {
            get => _hasAvailableUpdate;
            set
            {
                if (SetProperty(ref _hasAvailableUpdate, value))
                {
                    (UpdateSoftwareCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string VersionText
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private void OpenUserManagementDialog()
        {
            if (!AuthContext.CanManageUsers)
            {
                MessageBox.Show("Chỉ admin được quản lý tài khoản người dùng.", "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RefreshUsers();
            ClearUserForm();
            IsUserManagementDialogOpen = true;
        }

        private void RefreshUsers()
        {
            Users.Clear();
            foreach (var user in _dataService.GetUsers())
            {
                Users.Add(user);
            }
        }

        private void LoadSelectedUser()
        {
            if (SelectedUser == null)
            {
                return;
            }

            UserNameText = SelectedUser.UserName;
            UserDisplayNameText = SelectedUser.DisplayName;
            SelectedUserRole = SelectedUser.Role;
            IsUserActive = SelectedUser.IsActive;
            UserPasswordText = string.Empty;
        }

        private void ClearUserForm()
        {
            SelectedUser = null;
            UserNameText = string.Empty;
            UserDisplayNameText = string.Empty;
            SelectedUserRole = UserRoles.Count > 1 ? UserRoles[1] : Models.UserRoles.Officer;
            UserPasswordText = string.Empty;
            IsUserActive = true;
            OnPropertyChanged(nameof(IsEditingUser));
            OnPropertyChanged(nameof(UserSubmitButtonText));
        }

        private void EditUser(object parameter)
        {
            if (parameter is AppUser user)
            {
                SelectedUser = user;
            }
        }

        private void SaveUser()
        {
            if (!AuthContext.CanManageUsers)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(UserNameText) ||
                string.IsNullOrWhiteSpace(UserDisplayNameText) ||
                string.IsNullOrWhiteSpace(SelectedUserRole) ||
                (!IsEditingUser && string.IsNullOrWhiteSpace(UserPasswordText)))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập, họ tên, vai trò và mật khẩu khi tạo mới.", "Quản lý người dùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = new AppUser
            {
                Id = SelectedUser?.Id ?? 0,
                UserName = UserNameText,
                DisplayName = UserDisplayNameText,
                Role = SelectedUserRole,
                IsActive = IsUserActive
            };

            try
            {
                if (!_dataService.SaveUser(user, UserPasswordText))
                {
                    MessageBox.Show("Không thể lưu tài khoản. Vui lòng kiểm tra tên đăng nhập hoặc mật khẩu.", "Quản lý người dùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RefreshUsers();
                ClearUserForm();
                MessageBox.Show("Đã lưu tài khoản người dùng.", "Quản lý người dùng", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "SaveUser", ex, "Failed to save user.", UserNameText);
                MessageBox.Show($"Không thể lưu tài khoản.\n\nChi tiết: {ex.Message}", "Quản lý người dùng", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser(object parameter)
        {
            if (parameter is not AppUser user)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Khóa tài khoản {user.UserName}?",
                "Quản lý người dùng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (_dataService.DeleteUser(user.Id))
            {
                RefreshUsers();
                ClearUserForm();
            }
        }

        public void MoveCatalogValue(CatalogValueSetting source, CatalogValueSetting target)
        {
            if (source == null || target == null || source == target)
            {
                return;
            }

            var sourceIndex = CatalogValues.IndexOf(source);
            var targetIndex = CatalogValues.IndexOf(target);
            if (sourceIndex < 0 || targetIndex < 0)
            {
                return;
            }

            CatalogValues.Move(sourceIndex, targetIndex);
            for (var index = 0; index < CatalogValues.Count; index++)
            {
                CatalogValues[index].DisplayOrder = index + 1;
            }

            _dataService.UpdateCatalogItemOrders(CatalogValues.ToList());
            SelectedCatalogValue = source;
        }

        private CatalogGroupSetting CreateCatalogGroup(string type, string title, string description, string iconGlyph, string accentColor, string iconBackground)
        {
            return new CatalogGroupSetting
            {
                CatalogType = type,
                Title = title,
                Description = description,
                IconGlyph = iconGlyph,
                AccentColor = accentColor,
                IconBackground = iconBackground,
                ItemCount = 0
            };
        }

        private void SelectCatalogGroup(object parameter)
        {
            if (parameter is not CatalogGroupSetting group)
            {
                return;
            }

            foreach (var item in CatalogGroups)
            {
                item.IsSelected = item == group;
            }

            _selectedCatalogGroup = group;
            OnPropertyChanged(nameof(SelectedCatalogGroup));
            OnPropertyChanged(nameof(CatalogDialogTitle));
            ReloadCatalogValues();
        }

        private void OpenCatalogDialog(object parameter)
        {
            CatalogSearchText = string.Empty;
            SelectedCatalogStatusFilter = CatalogStatusFilters.Skip(1).FirstOrDefault() ?? CatalogStatusFilters.FirstOrDefault();
            SelectCatalogGroup(parameter);
            IsCatalogDialogOpen = true;
        }

        private void SelectCatalogValue(object parameter)
        {
            if (parameter is CatalogValueSetting value)
            {
                SelectedCatalogValue = value;
            }
        }

        private void DeleteCatalogValueForRow(object parameter)
        {
            if (parameter is CatalogValueSetting value)
            {
                SelectedCatalogValue = value;
                DeleteCatalogValue();
            }
        }

        private void SaveCatalogValue()
        {
            if (IsEditingCatalogValue)
            {
                UpdateCatalogValue();
                return;
            }

            AddCatalogValue();
        }

        private void CancelCatalogEdit()
        {
            SelectedCatalogValue = null;
            CatalogValueText = string.Empty;
        }

        private void RefreshSoftwareInfos()
        {
            SoftwareInfos.Clear();
            SoftwareInfos.Add(new SoftwareInfo { Label = "Phiên bản", Value = VersionText });
            SoftwareInfos.Add(new SoftwareInfo { Label = "Môi trường chạy", Value = RuntimeInformation.FrameworkDescription });
            SoftwareInfos.Add(new SoftwareInfo { Label = "Chế độ dữ liệu", Value = AppPathSettings.Current.IsClientMode ? "Máy trạm" : "Máy server" });
            SoftwareInfos.Add(new SoftwareInfo { Label = "URL máy server", Value = AppPathSettings.Current.AdminServerUrl });
            SoftwareInfos.Add(new SoftwareInfo { Label = "Đơn vị phát triển", Value = "minhthang3321@gmail.com" });
        }

        private void OpenGeneralSettingsDialog()
        {
            DatabasePathText = AppPathSettings.Current.DatabasePath;
            LogFolderText = AppPathSettings.Current.LogFolder;
            SelectedDataAccessMode = AppPathSettings.Current.DataAccessMode;
            AdminMachineNameText = AppPathSettings.Current.AdminMachineName;
            AdminServerUrlText = AppPathSettings.Current.AdminServerUrl;
            GeneralSettingsStatus = "MÃ¡y admin giá»¯ DB local vÃ  má»Ÿ API LAN. MÃ¡y tráº¡m chá»‰ nháº­p URL mÃ¡y admin, khÃ´ng dÃ¹ng DB local.";
            IsGeneralSettingsDialogOpen = true;
        }

        private void ChooseDatabasePath()
        {
            using var dialog = new Forms.SaveFileDialog
            {
                Title = "Chá»n Ä‘Æ°á»ng dáº«n cÆ¡ sá»Ÿ dá»¯ liá»‡u",
                Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
                FileName = Path.GetFileName(DatabasePathText),
                InitialDirectory = Directory.Exists(Path.GetDirectoryName(DatabasePathText))
                    ? Path.GetDirectoryName(DatabasePathText)
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                OverwritePrompt = false
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                DatabasePathText = dialog.FileName;
            }
        }

        private void ChooseLogFolder()
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Chá»n thÆ° má»¥c lÆ°u log",
                SelectedPath = Directory.Exists(LogFolderText)
                    ? LogFolderText
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                LogFolderText = dialog.SelectedPath;
            }
        }

        private void SaveGeneralSettings()
        {
            try
            {
                var databasePath = AppPathSettings.NormalizeDatabasePath(DatabasePathText);
                var logFolder = AppPathSettings.NormalizeLogFolder(LogFolderText);
                var dataAccessMode = AppPathSettings.NormalizeDataAccessMode(SelectedDataAccessMode);
                var adminServerUrl = AppPathSettings.NormalizeAdminServerUrl(AdminServerUrlText);
                var databaseFolder = Path.GetDirectoryName(databasePath);

                if (dataAccessMode == "Client" && !Uri.TryCreate(adminServerUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show(
                        "MÃ¡y tráº¡m pháº£i nháº­p URL mÃ¡y admin há»£p lá»‡, vÃ­ dá»¥ http://localhost:5055 khi test local hoáº·c http://192.168.1.10:5055 khi cháº¡y LAN.",
                        "CÃ i Ä‘áº·t dá»¯ liá»‡u trung tÃ¢m",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (dataAccessMode != "Client" && string.IsNullOrWhiteSpace(databaseFolder))
                {
                    MessageBox.Show("ÄÆ°á»ng dáº«n cÆ¡ sá»Ÿ dá»¯ liá»‡u khÃ´ng há»£p lá»‡.", "CÃ i Ä‘áº·t chung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dataAccessMode != "Client")
                {
                    Directory.CreateDirectory(databaseFolder);
                }

                Directory.CreateDirectory(logFolder);

                if (dataAccessMode != "Client" &&
                    !databasePath.Equals(_dataService.DatabasePath, StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(_dataService.DatabasePath) &&
                    !File.Exists(databasePath))
                {
                    _dataService.BackupDatabase(databasePath);
                }

                AppPathSettings.Save(new AppPathSettings
                {
                    DatabasePath = databasePath,
                    LogFolder = logFolder,
                    DataAccessMode = dataAccessMode,
                    AdminMachineName = AdminMachineNameText,
                    AdminServerUrl = adminServerUrl
                });

                DatabasePathText = databasePath;
                LogFolderText = logFolder;
                SelectedDataAccessMode = dataAccessMode;
                AdminServerUrlText = adminServerUrl;
                RefreshSoftwareInfos();
                GeneralSettingsStatus = databasePath.Equals(_dataService.DatabasePath, StringComparison.OrdinalIgnoreCase)
                    ? "ÄÃ£ lÆ°u cÃ i Ä‘áº·t. ÄÆ°á»ng dáº«n log má»›i cÃ³ hiá»‡u lá»±c ngay."
                    : "ÄÃ£ lÆ°u cÃ i Ä‘áº·t. Vui lÃ²ng khá»Ÿi Ä‘á»™ng láº¡i á»©ng dá»¥ng Ä‘á»ƒ dÃ¹ng Ä‘Æ°á»ng dáº«n DB má»›i.";

                MessageBox.Show(GeneralSettingsStatus, "CÃ i Ä‘áº·t chung", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "SaveGeneralSettings", ex, "Failed to save path settings.");
                GeneralSettingsStatus = "KhÃ´ng thá»ƒ lÆ°u cÃ i Ä‘áº·t Ä‘Æ°á»ng dáº«n.";
                MessageBox.Show($"KhÃ´ng thá»ƒ lÆ°u cÃ i Ä‘áº·t Ä‘Æ°á»ng dáº«n.\n{ex.Message}", "CÃ i Ä‘áº·t chung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetGeneralSettings()
        {
            DatabasePathText = AppPathSettings.DefaultDatabasePath;
            LogFolderText = AppPathSettings.DefaultLogFolder;
            SelectedDataAccessMode = "AdminHost";
            AdminMachineNameText = Environment.MachineName;
            AdminServerUrlText = "http://localhost:5055";
            GeneralSettingsStatus = "ÄÃ£ Ä‘Æ°a vá» cháº¿ Ä‘á»™ mÃ¡y admin giá»¯ DB. Báº¥m LÆ°u cÃ i Ä‘áº·t Ä‘á»ƒ Ã¡p dá»¥ng.";
        }

        private void OpenSystemLogDialog()
        {
            RefreshSystemLogs();
            IsSystemLogDialogOpen = true;
        }

        private void RefreshSystemLogs()
        {
            SystemLogs.Clear();
            foreach (var log in _dataService.GetSystemLogs())
            {
                SystemLogs.Add(log);
            }
        }

        private void PreviousCatalogPage()
        {
            if (CatalogCurrentPage <= 1)
            {
                return;
            }

            CatalogCurrentPage--;
            ApplyCatalogFilters();
        }

        private void NextCatalogPage()
        {
            if (CatalogCurrentPage >= CatalogTotalPages)
            {
                return;
            }

            CatalogCurrentPage++;
            ApplyCatalogFilters();
        }

        private void ReloadCatalogValues(int selectedId = 0)
        {
            _allCatalogValues.Clear();
            CatalogValues.Clear();
            if (SelectedCatalogGroup == null)
            {
                return;
            }

            foreach (var item in _dataService.GetCatalogItems(SelectedCatalogGroup.CatalogType))
            {
                _allCatalogValues.Add(item);
            }

            CatalogCurrentPage = 1;
            ApplyCatalogFilters(selectedId);
            RefreshCatalogGroupCounts();
        }

        private void ApplyCatalogFilters(int selectedId = 0)
        {
            if (CatalogValues == null || _allCatalogValues == null)
            {
                return;
            }

            var filtered = _allCatalogValues.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(CatalogSearchText))
            {
                filtered = filtered.Where(item =>
                    item.Name?.IndexOf(CatalogSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            if (SelectedCatalogStatusFilter == "Đang sử dụng")
            {
                filtered = filtered.Where(item => item.IsActive);
            }
            else if (SelectedCatalogStatusFilter == "Ngưng sử dụng")
            {
                filtered = filtered.Where(item => !item.IsActive);
            }

            var filteredItems = filtered.ToList();
            _catalogFilteredCount = filteredItems.Count;

            if (selectedId > 0)
            {
                var selectedIndex = filteredItems.FindIndex(item => item.Id == selectedId);
                if (selectedIndex >= 0)
                {
                    CatalogCurrentPage = (selectedIndex / CatalogDialogPageSize) + 1;
                }
            }

            if (CatalogCurrentPage > CatalogTotalPages)
            {
                CatalogCurrentPage = CatalogTotalPages;
            }

            CatalogValues.Clear();
            foreach (var item in filteredItems)
            {
                CatalogValues.Add(item);
            }

            SelectedCatalogValue = selectedId > 0
                ? CatalogValues.FirstOrDefault(item => item.Id == selectedId) ?? CatalogValues.FirstOrDefault()
                : null;
            OnPropertyChanged(nameof(CatalogTotalPages));
            OnPropertyChanged(nameof(CatalogPageText));
            OnPropertyChanged(nameof(CatalogRowsText));
            RaiseCatalogPageCommandState();
        }

        private void AddCatalogValue()
        {
            if (!ValidateCatalogInput())
            {
                return;
            }

            var newId = _dataService.AddCatalogItem(SelectedCatalogGroup.CatalogType, CatalogValueText);
            if (newId == 0)
            {
                MessageBox.Show("Giá trị này đã tồn tại hoặc không thể thêm mới.", "Danh mục nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ReloadCatalogValues();
            CatalogValueText = string.Empty;
            MessageBox.Show("Đã thêm mới danh mục.", "Danh mục nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateCatalogValue()
        {
            if (SelectedCatalogValue == null || !ValidateCatalogInput())
            {
                return;
            }

            if (!_dataService.UpdateCatalogItem(SelectedCatalogValue.Id, CatalogValueText))
            {
                MessageBox.Show("Không thể cập nhật. Vui lòng kiểm tra giá trị trùng lặp.", "Danh mục nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ReloadCatalogValues();
            CatalogValueText = string.Empty;
            MessageBox.Show("Đã cập nhật danh mục.", "Danh mục nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteCatalogValue()
        {
            if (SelectedCatalogValue == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Xóa \"{SelectedCatalogValue.Name}\" khỏi danh mục {SelectedCatalogGroup.Title}?",
                "Xóa danh mục",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (_dataService.DeleteCatalogItem(SelectedCatalogValue.Id))
            {
                ReloadCatalogValues();
                CatalogValueText = string.Empty;
            }
        }

        private bool ValidateCatalogInput()
        {
            if (SelectedCatalogGroup == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CatalogValueText))
            {
                return true;
            }

            MessageBox.Show("Vui lòng nhập tên danh mục.", "Danh mục nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void ChooseBackupFolder()
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Chá»n thÆ° má»¥c sao lÆ°u dá»¯ liá»‡u",
                SelectedPath = Directory.Exists(BackupFolder) ? BackupFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                BackupFolder = dialog.SelectedPath;
                BackupStatus = "ÄÃ£ chá»n thÆ° má»¥c sao lÆ°u";
            }
        }

        private string GetDatabaseSizeText()
        {
            if (!File.Exists(_dataService.DatabasePath))
            {
                return "Chưa có dữ liệu";
            }

            var bytes = new FileInfo(_dataService.DatabasePath).Length;
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / 1024d / 1024d:0.#} MB";
            }

            return $"{bytes / 1024d:0.#} KB";
        }

        private async Task BackupNowAsync()
        {
            try
            {
                var fileName = $"quanlyhoso_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var destinationPath = await Task.Run(() => _dataService.CreateBackupFile(fileName));
                LastBackupText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                BackupStatus = $"Đã sao lưu: {fileName}";
                MessageBox.Show($"Đã sao lưu dữ liệu trên máy server:\n{destinationPath}", "Sao lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "BackupNow", ex, "Failed to backup database.");
                BackupStatus = "Sao lưu không thành công";
                MessageBox.Show($"Không thể sao lưu dữ liệu.\n{ex.Message}", "Sao lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RestoreDataAsync()
        {
            using var dialog = new Forms.OpenFileDialog
            {
                Title = "Chá»n file sao lÆ°u Ä‘á»ƒ khÃ´i phá»¥c",
                Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
                Multiselect = false,
                InitialDirectory = Directory.Exists(BackupFolder)
                    ? BackupFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() != Forms.DialogResult.OK)
            {
                return;
            }

            var confirm = MessageBox.Show(
                "KhÃ´i phá»¥c sáº½ thay tháº¿ cÆ¡ sá»Ÿ dá»¯ liá»‡u hiá»‡n táº¡i báº±ng file Ä‘Ã£ chá»n. Há»‡ thá»‘ng sáº½ táº¡o má»™t báº£n sao lÆ°u an toÃ n trÆ°á»›c khi khÃ´i phá»¥c.\n\nBáº¡n cÃ³ muá»‘n tiáº¿p tá»¥c khÃ´ng?",
                "KhÃ´i phá»¥c dá»¯ liá»‡u",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(BackupFolder);
                var safetyBackupPath = Path.Combine(BackupFolder, $"quanlyhoso_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                var restorePath = dialog.FileName;
                await Task.Run(() => _dataService.RestoreDatabaseFromFile(restorePath, safetyBackupPath));
                LastBackupText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                BackupStatus = "ÄÃ£ khÃ´i phá»¥c dá»¯ liá»‡u";
                RefreshSoftwareInfos();
                RefreshCatalogGroupCounts();
                MessageBox.Show($"ÄÃ£ khÃ´i phá»¥c dá»¯ liá»‡u.\nBáº£n sao lÆ°u an toÃ n Ä‘Æ°á»£c lÆ°u táº¡i:\n{safetyBackupPath}", "KhÃ´i phá»¥c dá»¯ liá»‡u", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "RestoreData", ex, "Failed to restore database.");
                BackupStatus = "KhÃ´i phá»¥c khÃ´ng thÃ nh cÃ´ng";
                MessageBox.Show($"KhÃ´ng thá»ƒ khÃ´i phá»¥c dá»¯ liá»‡u.\n{ex.Message}", "KhÃ´i phá»¥c dá»¯ liá»‡u", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CheckUpdateAsync()
        {
            IsCheckingUpdate = true;
            HasAvailableUpdate = false;
            UpdateStatus = "Đang kiểm tra phiên bản mới trên GitHub...";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("QuanLyHoSo-Updater/1.0");

                var release = await GetLatestReleaseAsync(client);
                var latestVersion = NormalizeVersionText(release?.TagName);
                var currentVersion = NormalizeVersionText(VersionText);

                if (!Version.TryParse(latestVersion, out var latest) ||
                    !Version.TryParse(currentVersion, out var current))
                {
                    UpdateStatus = "Không đọc được số phiên bản từ GitHub Release. Vui lòng kiểm tra tag release, ví dụ v1.0.1.";
                    return;
                }

                _latestReleaseUrl = string.IsNullOrWhiteSpace(release?.HtmlUrl)
                    ? GitHubReleasesPageUrl
                    : release.HtmlUrl;
                _latestReleaseDownloadUrl = release?.Assets?
                    .Where(asset => !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
                    .FirstOrDefault(asset => asset.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                    ?.BrowserDownloadUrl;
                _latestReleaseVersion = latestVersion;

                if (latest > current)
                {
                    if (string.IsNullOrWhiteSpace(_latestReleaseDownloadUrl))
                    {
                        UpdateStatus = $"Có bản {latestVersion}, nhưng release chưa có file .zip để cập nhật tự động. Hãy upload bản publish .zip vào Assets.";
                        return;
                    }

                    HasAvailableUpdate = true;
                    UpdateStatus = $"Có bản cập nhật {latestVersion}. Phiên bản hiện tại là {currentVersion}. Bấm Cập nhật để tải và cài tự động.";
                    return;
                }

                UpdateStatus = $"Phiên bản {currentVersion} đang là bản mới nhất.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "CheckUpdate", ex, "Failed to check GitHub release.");
                UpdateStatus = "Không thể kiểm tra cập nhật. Vui lòng kiểm tra kết nối mạng hoặc GitHub Release của repo.";
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        private static async Task<GitHubReleaseInfo> GetLatestReleaseAsync(HttpClient client)
        {
            try
            {
                var latestJson = await client.GetStringAsync(GitHubLatestReleaseApiUrl);
                return JsonSerializer.Deserialize<GitHubReleaseInfo>(latestJson);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var releasesJson = await client.GetStringAsync(GitHubReleasesApiUrl);
                var releases = JsonSerializer.Deserialize<GitHubReleaseInfo[]>(releasesJson);
                return releases?.FirstOrDefault(release => !release.Draft);
            }
        }

        private async Task UpdateSoftwareAsync()
        {
            if (!HasAvailableUpdate || string.IsNullOrWhiteSpace(_latestReleaseDownloadUrl))
            {
                MessageBox.Show("Chưa có bản cập nhật mới. Vui lòng bấm Kiểm tra cập nhật để kiểm tra lại.", "Cập nhật phần mềm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"App sẽ tải bản {_latestReleaseVersion}, đóng chương trình, cài bản mới rồi mở lại.\n\nBạn có muốn cập nhật ngay không?",
                "Cập nhật phần mềm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            IsCheckingUpdate = true;
            UpdateStatus = $"Đang tải bản cập nhật {_latestReleaseVersion}...";

            try
            {
                var updateFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuanLyHoSo",
                    "Updates");
                Directory.CreateDirectory(updateFolder);

                var packagePath = Path.Combine(updateFolder, $"QuanLyHoSo-{_latestReleaseVersion}.zip");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("QuanLyHoSo-Updater/1.0");
                    using var response = await client.GetAsync(_latestReleaseDownloadUrl);
                    response.EnsureSuccessStatusCode();

                    await using var remoteStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = File.Create(packagePath);
                    await remoteStream.CopyToAsync(fileStream);
                }

                UpdateStatus = "Đã tải bản cập nhật. Đang khởi động trình cài đặt...";
                StartUpdaterAndShutdown(packagePath);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "UpdateSoftware", ex, "Failed to download or start update package.");
                UpdateStatus = "Không thể tải hoặc cài bản cập nhật. Vui lòng thử lại hoặc tải thủ công từ GitHub Release.";
                MessageBox.Show($"Không thể cập nhật tự động.\n\n{ex.Message}", "Cập nhật phần mềm", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

        private static void StartUpdaterAndShutdown(string packagePath)
        {
            var currentProcess = Process.GetCurrentProcess();
            var exePath = currentProcess.MainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "QuanLyHoSo.exe");
            var installDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptPath = Path.Combine(Path.GetTempPath(), $"QuanLyHoSo_Update_{Guid.NewGuid():N}.ps1");

            var script = BuildUpdaterScript(
                currentProcess.Id,
                packagePath,
                installDir,
                exePath);
            File.WriteAllText(scriptPath, script, Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {QuoteProcessArgument(scriptPath)}",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Application.Current.Shutdown();
        }

        private static string BuildUpdaterScript(int processId, string packagePath, string installDir, string exePath)
        {
            return $@"
$ErrorActionPreference = 'Stop'
$processId = {processId}
$packagePath = {QuotePowerShellString(packagePath)}
$installDir = {QuotePowerShellString(installDir)}
$exePath = {QuotePowerShellString(exePath)}
$extractDir = Join-Path ([System.IO.Path]::GetTempPath()) ('QuanLyHoSo_Update_' + [System.Guid]::NewGuid().ToString('N'))

Wait-Process -Id $processId -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 700
New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
Expand-Archive -LiteralPath $packagePath -DestinationPath $extractDir -Force

$sourceDir = $extractDir
$children = @(Get-ChildItem -LiteralPath $extractDir)
$directories = @($children | Where-Object {{ $_.PSIsContainer }})
$files = @($children | Where-Object {{ -not $_.PSIsContainer }})
if ($directories.Count -eq 1 -and $files.Count -eq 0) {{
    $sourceDir = $directories[0].FullName
}}

Copy-Item -Path (Join-Path $sourceDir '*') -Destination $installDir -Recurse -Force
Start-Process -FilePath $exePath
Remove-Item -LiteralPath $extractDir -Recurse -Force -ErrorAction SilentlyContinue
";
        }

        private static string QuotePowerShellString(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }

        private static string QuoteProcessArgument(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private static string NormalizeVersionText(string versionText)
        {
            return string.IsNullOrWhiteSpace(versionText)
                ? string.Empty
                : versionText.Trim().TrimStart('v', 'V');
        }

        private void RefreshCatalogGroupCounts()
        {
            var countsByType = _dataService.CountCatalogItemsByType();
            foreach (var group in CatalogGroups)
            {
                group.ItemCount = countsByType.TryGetValue(group.CatalogType, out var count) ? count : 0;
            }
        }

        private void RaiseCatalogCommandState()
        {
            (CancelCatalogEditCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UpdateCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void RaiseCatalogPageCommandState()
        {
            (PreviousCatalogPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NextCatalogPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private sealed class GitHubReleaseInfo
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; }

            [JsonPropertyName("draft")]
            public bool Draft { get; set; }

            [JsonPropertyName("assets")]
            public GitHubReleaseAsset[] Assets { get; set; }
        }

        private sealed class GitHubReleaseAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
