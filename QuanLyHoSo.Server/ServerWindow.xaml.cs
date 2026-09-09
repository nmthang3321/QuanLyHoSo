using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;

namespace QuanLyHoSo.Server
{
    public partial class ServerWindow : Window
    {
        private readonly AppDataService _dataService;
        private readonly DispatcherTimer _timer;
        private readonly Forms.NotifyIcon _trayIcon;
        private readonly Forms.ToolStripMenuItem _trayToggleItem;
        private readonly System.Drawing.Icon _trayAppIcon;
        private DateTime? _startedAt;
        private bool _statusPulseRunning;
        private bool _allowClose;
        private bool _trayHintShown;

        public ServerWindow(AppDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _startedAt = _dataService.IsLanServerRunning ? DateTime.Now : (DateTime?)null;

            _trayToggleItem = new Forms.ToolStripMenuItem("Dừng máy chủ", null, (_, __) => Dispatcher.Invoke(ToggleServer));
            var trayMenu = new Forms.ContextMenuStrip();
            trayMenu.Items.Add("Mở bảng điều khiển", null, (_, __) => Dispatcher.Invoke(ShowFromTray));
            trayMenu.Items.Add(_trayToggleItem);
            trayMenu.Items.Add(new Forms.ToolStripSeparator());
            trayMenu.Items.Add("Thoát máy chủ", null, (_, __) => Dispatcher.Invoke(RequestExit));

            _trayAppIcon = LoadTrayIcon();
            _trayIcon = new Forms.NotifyIcon
            {
                Icon = _trayAppIcon,
                Text = "Quản lý hồ sơ - Máy chủ",
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (_, __) => Dispatcher.Invoke(ShowFromTray);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => UpdateStatus();
            _timer.Start();

            LoadServerInformation();
            UpdateStatus();
        }

        private void LoadServerInformation()
        {
            ClientUrlText.Text = BuildClientUrl();
            MachineNameText.Text = Environment.MachineName;
        }

        private static System.Drawing.Icon LoadTrayIcon()
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
                if (icon != null)
                {
                    return icon;
                }
            }

            return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        }

        private static string BuildClientUrl()
        {
            var configured = new Uri(AppPathSettings.Current.AdminServerUrl);
            if (configured.Host != "0.0.0.0" && configured.Host != "*")
            {
                return configured.ToString().TrimEnd('/');
            }

            var address = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .FirstOrDefault(item => item.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(item));
            return $"{configured.Scheme}://{address ?? IPAddress.Loopback}:{configured.Port}";
        }

        private void UpdateStatus()
        {
            var isRunning = _dataService.IsLanServerRunning;
            HeaderStatusText.Text = isRunning ? "Đang hoạt động" : "Đã dừng";
            HeaderStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isRunning ? "#37D67A" : "#FF5C5C"));
            ToggleServerButton.Content = isRunning ? "Dừng máy chủ" : "Khởi động máy chủ";
            _trayToggleItem.Text = isRunning ? "Dừng máy chủ" : "Khởi động máy chủ";
            _trayIcon.Text = isRunning ? "Quản lý hồ sơ - Đang hoạt động" : "Quản lý hồ sơ - Đã dừng";
            ConnectedClientsText.Text = $"{_dataService.ConnectedClientCount} máy";
            UpdateStatusPulse(isRunning);

            if (!isRunning || !_startedAt.HasValue)
            {
                UptimeText.Text = "—";
                return;
            }

            var uptime = DateTime.Now - _startedAt.Value;
            UptimeText.Text = uptime.TotalDays >= 1
                ? $"{(int)uptime.TotalDays} ngày {uptime:hh\\:mm\\:ss}"
                : uptime.ToString(@"hh\:mm\:ss");
        }

        private void UpdateStatusPulse(bool isRunning)
        {
            if (isRunning && !_statusPulseRunning)
            {
                HeaderStatusDot.BeginAnimation(
                    OpacityProperty,
                    new DoubleAnimation(1, 0.3, TimeSpan.FromSeconds(1.4))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
                _statusPulseRunning = true;
            }
            else if (!isRunning && _statusPulseRunning)
            {
                HeaderStatusDot.BeginAnimation(OpacityProperty, null);
                HeaderStatusDot.Opacity = 1;
                _statusPulseRunning = false;
            }
        }

        private void ToggleServer_Click(object sender, RoutedEventArgs e) => ToggleServer();

        private void ToggleServer()
        {
            try
            {
                if (_dataService.IsLanServerRunning)
                {
                    _dataService.StopLanServer();
                    _startedAt = null;
                }
                else
                {
                    _dataService.StartLanServer();
                    _startedAt = DateTime.Now;
                }

                UpdateStatus();
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServerUI", "ToggleServer", ex, "Cannot change server status.");
                MessageBox.Show(ex.Message, "Không thể thay đổi trạng thái", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = $"quanlyhoso_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var backupPath = _dataService.CreateServerBackupFile(fileName);
                MessageBox.Show($"Đã sao lưu thành công:\n{backupPath}", "Sao lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ServerUI", "Backup", ex, "Manual backup failed.");
                MessageBox.Show(ex.Message, "Sao lưu thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyUrl_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ClientUrlText.Text);
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Path.GetDirectoryName(AppPathSettings.Current.DatabasePath));
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(AppPathSettings.Current.LogFolder);
        }

        private static void OpenFolder(string folder)
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                HideToTray();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            _timer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayAppIcon.Dispose();
            _dataService.StopLanServer();
        }

        private void HideToTray()
        {
            Hide();
            if (_trayHintShown)
            {
                return;
            }

            _trayHintShown = true;
            _trayIcon.ShowBalloonTip(
                3000,
                "Máy chủ vẫn đang hoạt động",
                "Nhấp đúp biểu tượng ở khay hệ thống để mở lại bảng điều khiển.",
                Forms.ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => RequestExit();

        private void RequestExit()
        {
            var answer = MessageBox.Show(
                "Thoát sẽ dừng máy chủ và các máy trạm sẽ mất kết nối. Bạn có chắc chắn không?",
                "Thoát máy chủ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            _allowClose = true;
            Close();
            Application.Current.Shutdown();
        }
    }
}
