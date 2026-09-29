using System;
using System.Reflection;

namespace QuanLyHoSo.Infrastructure.Network
{
    public static class LanProtocolVersion
    {
        public const string ClientVersionHeader = "X-QuanLyHoSo-Version";
        public const string ActualClientVersionHeader = "X-QuanLyHoSo-Client-Version";

        public static string Current => GetProductVersion(Assembly.GetEntryAssembly());

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
