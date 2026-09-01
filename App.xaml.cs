using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Network;

namespace QuanLyHoSo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogger.Info("Application", "Startup", "Application started.");
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("Application", "Exit", $"Application exited with code {e.ApplicationExitCode}.");
            base.OnExit(e);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("Application", "DispatcherUnhandledException", e.Exception, "Unhandled UI exception.");
            if (e.Exception is LanServerUnavailableException)
            {
                MessageBox.Show(
                    e.Exception.Message,
                    "Không kết nối được máy server",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            MessageBox.Show(
                "Ứng dụng gặp lỗi chưa xử lý. Vui lòng gửi file log cho bộ phận hỗ trợ.",
                "Lỗi ứng dụng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppLogger.Error("Application", "UnhandledException", e.ExceptionObject as Exception, "Unhandled application exception.");
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.Error("Application", "UnobservedTaskException", e.Exception, "Unhandled background task exception.");
            e.SetObserved();
        }
    }
}
