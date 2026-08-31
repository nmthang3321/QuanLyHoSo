using System;
using System.IO;
using System.Text.Json;

namespace QuanLyHoSo.Infrastructure.Configuration
{
    public sealed class AppPathSettings
    {
        private const string SettingsFileName = "path-settings.json";
        private static readonly object SyncRoot = new object();
        private static AppPathSettings _current;

        public string DatabasePath { get; set; }
        public string LogFolder { get; set; }

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

            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(SettingsFolder, SettingsFileName), json);

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
                LogFolder = DefaultLogFolder
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
                return settings;
            }
            catch
            {
                return defaults;
            }
        }
    }
}
