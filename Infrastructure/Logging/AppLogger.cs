using System;
using System.Globalization;
using System.IO;
using QuanLyHoSo.Infrastructure.Configuration;

namespace QuanLyHoSo.Infrastructure.Logging
{
    public static class AppLogger
    {
        private static readonly object SyncRoot = new object();

        public static string LogFolder => AppPathSettings.Current.LogFolder;

        public static void Info(string module, string action, string message, string recordCode = null, string correlationId = null)
        {
            Write("INFO", module, action, message, null, recordCode, correlationId);
        }

        public static void Warning(string module, string action, string message, Exception exception = null, string recordCode = null, string correlationId = null)
        {
            Write("WARN", module, action, message, exception, recordCode, correlationId);
        }

        public static void Error(string module, string action, Exception exception, string message = null, string recordCode = null, string correlationId = null)
        {
            Write("ERROR", module, action, message ?? exception?.Message, exception, recordCode, correlationId);
        }

        private static void Write(string level, string module, string action, string message, Exception exception, string recordCode, string correlationId)
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                var logPath = Path.Combine(LogFolder, $"quanlyhoso-{DateTime.Now:yyyyMMdd}.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var id = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;
                var line = string.Join(" | ",
                    timestamp,
                    level,
                    id,
                    Environment.UserName,
                    Clean(module),
                    Clean(action),
                    Clean(recordCode),
                    Clean(message));

                if (exception != null)
                {
                    line += Environment.NewLine + exception;
                }

                lock (SyncRoot)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine, System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never break the main user workflow.
            }
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Replace(Environment.NewLine, " ").Replace("|", "/").Trim();
        }
    }
}
