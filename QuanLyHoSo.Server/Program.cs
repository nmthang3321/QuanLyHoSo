using System;
using System.IO;
using System.Threading;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;

namespace QuanLyHoSo.Server
{
    internal static class Program
    {
        private static readonly ManualResetEventSlim ShutdownSignal = new ManualResetEventSlim(false);

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            AppLogger.Info("Server", "Startup", "QuanLyHoSo server starting.");

            try
            {
                var options = ServerOptions.Parse(args);
                options.PrepareSampleDatabase();
                AppPathSettings.UseServerMode(options.DatabasePath, options.LogFolder, options.AdminServerUrl);
                AppDataService.Instance.Initialize();

                Console.WriteLine("QuanLyHoSo server is running.");
                Console.WriteLine($"API: {AppPathSettings.Current.AdminServerUrl}");
                Console.WriteLine($"Database: {AppPathSettings.Current.DatabasePath}");
                Console.WriteLine("Press Ctrl+C to stop.");

                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    ShutdownSignal.Set();
                };

                ShutdownSignal.Wait();
                AppLogger.Info("Server", "Shutdown", "QuanLyHoSo server stopped.");
                return 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Server", "Startup", ex, "QuanLyHoSo server failed.");
                Console.Error.WriteLine(ex.Message);
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

                var sampleSourcePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "SampleData",
                    "quanlyhoso-demo.db");
                if (!File.Exists(sampleSourcePath))
                {
                    throw new FileNotFoundException(
                        "Không tìm thấy database mẫu đi kèm ứng dụng.",
                        sampleSourcePath);
                }

                var sampleDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuanLyHoSo",
                    "Data");
                Directory.CreateDirectory(sampleDataFolder);

                DatabasePath = Path.Combine(sampleDataFolder, "quanlyhoso-sample.db");
                File.Copy(sampleSourcePath, DatabasePath, true);
                Console.WriteLine("Đã khởi tạo lại database chạy mẫu.");
            }
        }
    }
}
