namespace QuanLyHoSo.Models
{
    public sealed class DashboardMetric
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Delta { get; set; }
        public string DeltaAmount { get; set; }
        public string DeltaDescription { get; set; }
        public string DeltaGlyph { get; set; }
        public string DeltaColor { get; set; }
        public string IconGlyph { get; set; }
        public string AccentColor { get; set; }
        public string FilterKey { get; set; }
        public bool IsSelected { get; set; }
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

    public sealed class TrendStat
    {
        public string Label { get; set; }
        public int ReceivedCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ReceivedHeight { get; set; }
        public double ReceivedBarTop { get; set; }
        public double ResolvedPointY { get; set; }
        public double ResolvedPointTop { get; set; }
        public double ResolvedPointLeft { get; set; }
    }

    public sealed class TrendAxisTick
    {
        public string Label { get; set; }
        public double Top { get; set; }
    }

    public sealed class RecentRecord
    {
        public int Index { get; set; }
        public string RecordCode { get; set; }
        public string SenderName { get; set; }
        public string AreaName { get; set; }
        public string CaseType { get; set; }
        public string Field { get; set; }
        public string ReceivedDate { get; set; }
        public string Status { get; set; }
        public string UpdatedAt { get; set; }
        public string ProcessorName { get; set; }
    }
}
