using System.Collections.Generic;

namespace QuanLyHoSo.Models
{
    public sealed class StaffTrackingMetric
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Note { get; set; }
        public string IconGlyph { get; set; }
        public string AccentColor { get; set; }
        public string BackgroundColor { get; set; }
        public string FilterKey { get; set; }
        public bool IsSelected { get; set; }
    }

    public sealed class StaffPerformanceRow
    {
        public string Initials { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int AssignedCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CompletedCount { get; set; }
        public int DueSoonCount { get; set; }
        public int OverdueCount { get; set; }
        public string AverageProcessingTimeText { get; set; }
        public string OnTimeRateText { get; set; }
        public string OnTimeRateColor { get; set; }
        public int KpiPercent { get; set; }
        public string KpiStatus { get; set; }
        public string KpiStatusBackground { get; set; }
        public string KpiStatusForeground { get; set; }
    }

    public sealed class StaffWorkRecord
    {
        public string RecordCode { get; set; }
        public string CaseType { get; set; }
        public string DeadlineText { get; set; }
        public string DeadlineStatus { get; set; }
        public string StatusColor { get; set; }
    }

    public sealed class StaffNotification
    {
        public int Id { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public string ReceivedText { get; set; }
        public bool IsUnread { get; set; }
        public string FontWeightText => IsUnread ? "Bold" : "Normal";
        public string BackgroundColor => IsUnread ? "#FFF7ED" : "#F8FAFD";
        public string BorderColor => IsUnread ? "#FDBA74" : "#DDE7F5";
    }

    public sealed class StaffNotificationPage
    {
        public IReadOnlyList<StaffNotification> Items { get; set; }
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
    }

    public sealed class StaffBarStat
    {
        public string StaffName { get; set; }
        public int OnTimePercent { get; set; }
        public int KpiPercent { get; set; }
        public int OnTimeHeight { get; set; }
        public int KpiHeight { get; set; }
    }
}
