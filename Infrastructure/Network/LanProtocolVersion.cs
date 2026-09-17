using System;
using System.Reflection;

namespace QuanLyHoSo.Infrastructure.Network
{
    public static class LanProtocolVersion
    {
        public const string ClientVersionHeader = "X-QuanLyHoSo-Version";

        public static string Current => GetProductVersion(Assembly.GetEntryAssembly());

        public static bool IsCompatible(string clientVersion)
        {
            return string.Equals(Normalize(clientVersion), Current, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildMismatchMessage(string clientVersion)
        {
            var receivedVersion = string.IsNullOrWhiteSpace(clientVersion) ? "không xác định" : clientVersion.Trim();
            return $"Phiên bản Client ({receivedVersion}) không tương thích với Server ({Current}). Vui lòng cập nhật Client lên phiên bản {Current}.";
        }

        private static string GetProductVersion(Assembly assembly)
        {
            var informationalVersion = assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?
                .Split('+')[0];
            var normalizedInformationalVersion = Normalize(informationalVersion);
            if (!string.IsNullOrWhiteSpace(normalizedInformationalVersion))
            {
                return normalizedInformationalVersion;
            }

            return Normalize(assembly?.GetName().Version?.ToString()) ?? "1.0.0";
        }

        private static string Normalize(string version)
        {
            if (!Version.TryParse(version?.Trim(), out var parsed))
            {
                return null;
            }

            return $"{parsed.Major}.{parsed.Minor}.{Math.Max(parsed.Build, 0)}";
        }
    }
}
