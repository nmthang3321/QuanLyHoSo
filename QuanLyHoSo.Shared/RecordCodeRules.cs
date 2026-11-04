namespace QuanLyHoSo.Models
{
    public static class RecordCodeRules
    {
        public static string Normalize(string recordCode)
        {
            return (recordCode ?? string.Empty).Trim();
        }
    }
}
