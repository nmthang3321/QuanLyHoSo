using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
        private const string GitHubLatestReleaseApiUrl = "https://api.github.com/repos/nmthang3321/QuanLyHoSo/releases/latest";
        private const string GitHubReleasesApiUrl = "https://api.github.com/repos/nmthang3321/QuanLyHoSo/releases";
        private const string GitHubReleasesPageUrl = "https://github.com/nmthang3321/QuanLyHoSo/releases/latest";

        private readonly AppDataService _dataService;
        private CatalogGroupSetting _selectedCatalogGroup;
        private CatalogValueSetting _selectedCatalogValue;
        private string _catalogValueText;
        private string _backupFolder;
        private string _backupStatus;
        private string _updateStatus;
        private string _lastBackupText;
        private string _latestReleaseUrl;
        private string _latestReleaseDownloadUrl;
        private string _latestReleaseVersion;
        private bool _isCheckingUpdate;
        private bool _hasAvailableUpdate;

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
                new SoftwareInfo { Label = "Nguồn cập nhật", Value = "GitHub Releases" },
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
            CheckUpdateCommand = new RelayCommand(async () => await CheckUpdateAsync(), () => !IsCheckingUpdate);
            UpdateSoftwareCommand = new RelayCommand(async () => await UpdateSoftwareAsync(), () => HasAvailableUpdate && !IsCheckingUpdate);

            BackupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Backup");
            LastBackupText = "Chưa có bản sao lưu trong phiên này";
            BackupStatus = "Sẵn sàng sao lưu dữ liệu";
            UpdateStatus = "Chưa kiểm tra cập nhật";
            _latestReleaseUrl = GitHubReleasesPageUrl;

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
                MessageBox.Show("Chưa có bản cập nhật mới. Vui lòng bấm Check update để kiểm tra lại.", "Cập nhật phần mềm", MessageBoxButton.OK, MessageBoxImage.Information);
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
            (UpdateCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCatalogValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
