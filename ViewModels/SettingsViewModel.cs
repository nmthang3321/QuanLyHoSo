using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;
using Forms = System.Windows.Forms;

namespace QuanLyHoSo.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private CatalogGroupSetting _selectedCatalogGroup;
        private CatalogValueSetting _selectedCatalogValue;
        private string _catalogValueText;
        private string _backupFolder;
        private string _backupStatus;
        private string _updateStatus;
        private string _lastBackupText;

        public SettingsViewModel()
        {
            _dataService = AppDataService.Instance;

            CatalogGroups = new ObservableCollection<CatalogGroupSetting>
            {
                CreateCatalogGroup("ReceiveSource", "Nguồn tiếp nhận", "Dùng trong form nhập hồ sơ", "\uE77B", "#0B5CFF"),
                CreateCatalogGroup("CaseType", "Loại vụ việc", "Phân loại bản chất hồ sơ", "\uE8A5", "#1FA24A"),
                CreateCatalogGroup("Field", "Lĩnh vực", "Lọc, thống kê và xuất dữ liệu", "\uE8F9", "#7147D8"),
                CreateCatalogGroup("ContentGroup", "Nhóm nội dung", "Nhóm hóa nội dung phản ánh", "\uE8FD", "#D18A00"),
                CreateCatalogGroup("Priority", "Mức độ ưu tiên", "Dùng cho ưu tiên và mức độ xử lý", "\uE7BA", "#E85D04"),
                CreateCatalogGroup("ExpectedHandlingMethod", "Hướng xử lý", "Định hướng xử lý dự kiến", "\uE9D9", "#00856F")
            };

            CatalogValues = new ObservableCollection<CatalogValueSetting>();
            SoftwareInfos = new ObservableCollection<SoftwareInfo>
            {
                new SoftwareInfo { Label = "Phiên bản hiện tại", Value = VersionText },
                new SoftwareInfo { Label = "Khu vực sử dụng", Value = "An Giang" },
                new SoftwareInfo { Label = "Cơ sở dữ liệu", Value = "SQLite local" },
                new SoftwareInfo { Label = "Đường dẫn DB", Value = _dataService.DatabasePath },
                new SoftwareInfo { Label = "Đường dẫn log", Value = AppLogger.LogFolder }
            };

            SelectCatalogGroupCommand = new RelayCommand(SelectCatalogGroup);
            AddCatalogValueCommand = new RelayCommand(AddCatalogValue);
            UpdateCatalogValueCommand = new RelayCommand(UpdateCatalogValue, () => SelectedCatalogValue != null);
            DeleteCatalogValueCommand = new RelayCommand(DeleteCatalogValue, () => SelectedCatalogValue != null);
            ChooseBackupFolderCommand = new RelayCommand(ChooseBackupFolder);
            BackupNowCommand = new RelayCommand(BackupNow);
            RestoreDataCommand = new RelayCommand(ShowRestoreNotice);
            CheckUpdateCommand = new RelayCommand(CheckUpdate);
            UpdateSoftwareCommand = new RelayCommand(UpdateSoftware);

            BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Backup");
            LastBackupText = "Chưa có bản sao lưu trong phiên này";
            BackupStatus = "Sẵn sàng sao lưu dữ liệu";
            UpdateStatus = "Chưa kiểm tra cập nhật";

            SelectCatalogGroup(CatalogGroups.FirstOrDefault());
        }

        public ObservableCollection<CatalogGroupSetting> CatalogGroups { get; }
        public ObservableCollection<CatalogValueSetting> CatalogValues { get; }
        public ObservableCollection<SoftwareInfo> SoftwareInfos { get; }

        public ICommand SelectCatalogGroupCommand { get; }
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
                    RaiseCatalogCommandState();
                }
            }
        }

        public string CatalogValueText
        {
            get => _catalogValueText;
            set => SetProperty(ref _catalogValueText, value);
        }

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

        public string VersionText => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

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

        private CatalogGroupSetting CreateCatalogGroup(string type, string title, string description, string iconGlyph, string accentColor)
        {
            return new CatalogGroupSetting
            {
                CatalogType = type,
                Title = title,
                Description = description,
                IconGlyph = iconGlyph,
                AccentColor = accentColor,
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
            ReloadCatalogValues();
        }

        private void ReloadCatalogValues()
        {
            CatalogValues.Clear();
            if (SelectedCatalogGroup == null)
            {
                return;
            }

            foreach (var item in _dataService.GetCatalogItems(SelectedCatalogGroup.CatalogType))
            {
                CatalogValues.Add(item);
            }

            SelectedCatalogValue = CatalogValues.FirstOrDefault();
            RefreshCatalogGroupCounts();
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
            SelectedCatalogValue = CatalogValues.FirstOrDefault(item => item.Id == newId);
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

            var updatedId = SelectedCatalogValue.Id;
            ReloadCatalogValues();
            SelectedCatalogValue = CatalogValues.FirstOrDefault(item => item.Id == updatedId);
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
                Description = "Chọn thư mục sao lưu dữ liệu",
                SelectedPath = Directory.Exists(BackupFolder) ? BackupFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                BackupFolder = dialog.SelectedPath;
                BackupStatus = "Đã chọn thư mục sao lưu";
            }
        }

        private void BackupNow()
        {
            try
            {
                Directory.CreateDirectory(BackupFolder);
                var fileName = $"quanlyhoso_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var destinationPath = Path.Combine(BackupFolder, fileName);
                File.Copy(_dataService.DatabasePath, destinationPath, overwrite: false);
                LastBackupText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                BackupStatus = $"Đã sao lưu: {fileName}";
                MessageBox.Show($"Đã sao lưu dữ liệu vào:\n{destinationPath}", "Sao lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Settings", "BackupNow", ex, "Failed to backup database.");
                BackupStatus = "Sao lưu không thành công";
                MessageBox.Show($"Không thể sao lưu dữ liệu.\n{ex.Message}", "Sao lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowRestoreNotice()
        {
            MessageBox.Show("Chức năng khôi phục sẽ được thực hiện ở bước riêng để tránh ghi đè nhầm dữ liệu đang dùng.", "Khôi phục dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CheckUpdate()
        {
            UpdateStatus = $"Phiên bản {VersionText} đang là bản mới nhất trong cấu hình hiện tại.";
        }

        private void UpdateSoftware()
        {
            MessageBox.Show("Chưa cấu hình máy chủ cập nhật. Khi có gói phát hành, chức năng này sẽ tải và cài đặt phiên bản mới tại đây.", "Cập nhật phần mềm", MessageBoxButton.OK, MessageBoxImage.Information);
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
            (UpdateCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
