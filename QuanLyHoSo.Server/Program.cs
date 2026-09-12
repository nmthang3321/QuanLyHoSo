using System;
using System.IO;
using System.Windows;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;

namespace QuanLyHoSo.Server
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            AppLogger.Info("Server", "Startup", "QuanLyHoSo server starting.");

            try
            {
                var options = ServerOptions.Parse(args);
                options.PrepareSampleDatabase();
                AppPathSettings.UseServerMode(options.DatabasePath, options.LogFolder, options.AdminServerUrl);
                AppDataService.Instance.Initialize();

                var application = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                var window = new ServerWindow(AppDataService.Instance);
                application.Run(window);

                AppDataService.Instance.Shutdown();
                AppLogger.Info("Server", "Shutdown", "QuanLyHoSo server stopped.");
                return 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Server", "Startup", ex, "QuanLyHoSo server failed.");
                MessageBox.Show(
                    $"Không thể khởi động máy chủ.\n\n{ex.Message}",
                    "Quản lý hồ sơ - Server",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return 1;
            }
        }

        private sealed class ServerOptions
        {
            public string DatabasePath { get; private set; }
            public string LogFolder { get; private set; }
            public string AdminServerUrl { get; private set; }
            public bool UseSampleData { get; private set; }

            public static ServerOptions Parse(string[] args)
            {
                var options = new ServerOptions();
                for (var index = 0; index < (args?.Length ?? 0); index++)
                {
                    var arg = args[index];
                    if (string.Equals(arg, "--database", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                    {
                        options.DatabasePath = args[++index];
                    }
                    else if (string.Equals(arg, "--log-folder", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                    {
                        options.LogFolder = args[++index];
                    }
                    else if (string.Equals(arg, "--url", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                    {
                        options.AdminServerUrl = args[++index];
                    }
                    else if (string.Equals(arg, "--sample-data", StringComparison.OrdinalIgnoreCase))
                    {
                        options.UseSampleData = true;
                    }
                }

                return options;
            }

            public void PrepareSampleDatabase()
            {
                if (!UseSampleData)
                {
                    return;
                }

                var sampleSourcePath = Path.Combine(AppContext.BaseDirectory, "SampleData", "quanlyhoso-demo.db");
                if (!File.Exists(sampleSourcePath))
                {
                    throw new FileNotFoundException("Không tìm thấy database mẫu đi kèm ứng dụng.", sampleSourcePath);
                }

                var sampleDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuanLyHoSo",
                    "Data");
                Directory.CreateDirectory(sampleDataFolder);

                DatabasePath = Path.Combine(sampleDataFolder, "quanlyhoso-sample.db");
                File.Copy(sampleSourcePath, DatabasePath, true);
                AppLogger.Info("Server", "SampleData", "Sample database was recreated.");
            }
        }
    }
}
