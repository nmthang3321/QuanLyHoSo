using System;
using System.Globalization;
using System.IO;
using QuanLyHoSo.Infrastructure.Configuration;

namespace QuanLyHoSo.Infrastructure.Logging
{
    public static class AppLogger
    {
        private const int LogRetentionDays = 30;
        private const string LogFilePrefix = "quanlyhoso-";
        private static readonly object SyncRoot = new object();
        private static DateTime _lastCleanupDate = DateTime.MinValue;
        private static string _lastCleanupFolder;

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
                var logFolder = LogFolder;
                Directory.CreateDirectory(logFolder);
                var now = DateTime.Now;
                var logPath = Path.Combine(logFolder, $"{LogFilePrefix}{now:yyyyMMdd}.log");
                var timestamp = now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
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
                    DeleteExpiredLogsIfNeeded(logFolder, now.Date);
                    File.AppendAllText(logPath, line + Environment.NewLine, System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never break the main user workflow.
            }
        }

        private static void DeleteExpiredLogsIfNeeded(string logFolder, DateTime today)
        {
            if (_lastCleanupDate == today
                && string.Equals(_lastCleanupFolder, logFolder, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastCleanupDate = today;
            _lastCleanupFolder = logFolder;
            var oldestDateToKeep = today.AddDays(-(LogRetentionDays - 1));

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(logFolder, $"{LogFilePrefix}*.log", SearchOption.TopDirectoryOnly))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    if (!fileNameWithoutExtension.StartsWith(LogFilePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var datePart = fileNameWithoutExtension.Substring(LogFilePrefix.Length);
                    if (!DateTime.TryParseExact(
                            datePart,
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var logDate)
                        || logDate >= oldestDateToKeep)
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // A locked or protected old log must not interrupt current logging.
                    }
                }
            }
            catch
            {
                // Retention cleanup must never interrupt current logging.
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
