using System.Text.RegularExpressions;

namespace QuanLyHoSo.Models
{
    public static class RecordCodeRules
    {
        public const string FormatExample = "HS-2026-000123";

        private static readonly Regex ValidPattern = new Regex(
            @"^HS-\d{4}-\d{6}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Normalize(string recordCode)
        {
            return (recordCode ?? string.Empty).Trim().ToUpperInvariant();
        }

        public static bool IsValid(string recordCode)
        {
            return ValidPattern.IsMatch(Normalize(recordCode));
        }
    }
}
