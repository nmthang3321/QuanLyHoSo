using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuanLyHoSo.Infrastructure.Configuration
{
    public sealed class AppPathSettings
    {
        private const string SettingsFileName = "path-settings.json";
        private static readonly object SyncRoot = new object();
        private static AppPathSettings _current;

        public string DatabasePath { get; set; }
        public string LogFolder { get; set; }
        public string DataAccessMode { get; set; }
        public string AdminMachineName { get; set; }
        public string AdminServerUrl { get; set; }

        [JsonIgnore]
        public bool IsClientMode => string.Equals(DataAccessMode, "Client", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsAdminHostMode => !IsClientMode;

        public static AppPathSettings Current
        {
            get
            {
                lock (SyncRoot)
                {
                    return _current ??= Load();
                }
            }
        }

        public static string SettingsFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyHoSo",
            "Settings");

        public static string DefaultDatabasePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyHoSo",
            "Data",
            "quanlyhoso.db");

        public static string DefaultLogFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuanLyHoSo",
            "Logs");

        public static void Save(AppPathSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.DatabasePath = NormalizeDatabasePath(settings.DatabasePath);
            settings.LogFolder = NormalizeLogFolder(settings.LogFolder);
            settings.DataAccessMode = NormalizeDataAccessMode(settings.DataAccessMode);
            settings.AdminMachineName = settings.AdminMachineName?.Trim() ?? string.Empty;
            settings.AdminServerUrl = NormalizeAdminServerUrl(settings.AdminServerUrl);

            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(SettingsFolder, SettingsFileName), json);

            lock (SyncRoot)
            {
                _current = settings;
            }
        }

        public static void UseServerMode(string databasePath = null, string logFolder = null, string adminServerUrl = null)
        {
            var current = Current;
            var settings = new AppPathSettings
            {
                DatabasePath = NormalizeDatabasePath(databasePath ?? current.DatabasePath),
                LogFolder = NormalizeLogFolder(logFolder ?? current.LogFolder),
                DataAccessMode = "AdminHost",
                AdminMachineName = Environment.MachineName,
                AdminServerUrl = NormalizeAdminServerUrl(adminServerUrl ?? current.AdminServerUrl)
            };

            lock (SyncRoot)
            {
                _current = settings;
            }
        }

        public static string NormalizeDatabasePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultDatabasePath;
            }

            var path = Environment.ExpandEnvironmentVariables(value.Trim());
            if (Directory.Exists(path))
            {
                path = Path.Combine(path, "quanlyhoso.db");
            }

            return Path.GetFullPath(path);
        }

        public static string NormalizeLogFolder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultLogFolder;
            }

            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim()));
        }

        private static AppPathSettings Load()
        {
            var defaults = new AppPathSettings
            {
                DatabasePath = DefaultDatabasePath,
                LogFolder = DefaultLogFolder,
                DataAccessMode = "Client",
                AdminMachineName = Environment.MachineName,
                AdminServerUrl = "http://localhost:5055"
            };

            var settingsPath = Path.Combine(SettingsFolder, SettingsFileName);
            if (!File.Exists(settingsPath))
            {
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppPathSettings>(json) ?? defaults;
                settings.DatabasePath = NormalizeDatabasePath(settings.DatabasePath);
                settings.LogFolder = NormalizeLogFolder(settings.LogFolder);
                settings.DataAccessMode = NormalizeDataAccessMode(settings.DataAccessMode);
                settings.AdminMachineName = string.IsNullOrWhiteSpace(settings.AdminMachineName)
                    ? Environment.MachineName
                    : settings.AdminMachineName.Trim();
                settings.AdminServerUrl = NormalizeAdminServerUrl(settings.AdminServerUrl);
                return settings;
            }
            catch
            {
                return defaults;
            }
        }

        public static string NormalizeDataAccessMode(string value)
        {
            return string.Equals(value, "AdminHost", StringComparison.OrdinalIgnoreCase)
                ? "AdminHost"
                : "Client";
        }

        public static bool IsNetworkDatabasePath(string databasePath)
        {
            return !string.IsNullOrWhiteSpace(databasePath) && databasePath.Trim().StartsWith(@"\\", StringComparison.Ordinal);
        }

        public static string NormalizeAdminServerUrl(string value)
        {
            var url = string.IsNullOrWhiteSpace(value)
                ? "http://localhost:5055"
                : value.Trim().TrimEnd('/');

            return url;
        }
    }
}
