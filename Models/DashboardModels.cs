namespace QuanLyHoSo.Models
{
    public sealed class DashboardMetric
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Delta { get; set; }
        public string IconGlyph { get; set; }
        public string AccentColor { get; set; }
    }

    public sealed class StatusStat
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string Percentage { get; set; }
        public int Width { get; set; }
        public double StartAngle { get; set; }
        public double SweepAngle { get; set; }
        public string Color { get; set; }
    }

    public sealed class AreaStat
    {
        public string AreaName { get; set; }
        public int Count { get; set; }
        public int Width { get; set; }
    }

    public sealed class RecentRecord
    {
        public int Index { get; set; }
        public string RecordCode { get; set; }
        public string SenderName { get; set; }
        public string AreaName { get; set; }
        public string Status { get; set; }
        public string UpdatedAt { get; set; }
        public string ProcessorName { get; set; }
    }
}
