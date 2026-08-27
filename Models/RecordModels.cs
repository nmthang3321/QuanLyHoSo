namespace QuanLyHoSo.Models
{
    using System.Collections.Generic;

    public sealed class AttachmentDraft
    {
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string FilePath { get; set; }
    }

    public sealed class RecordFormDraft
    {
        public string RecordCode { get; set; }
        public string ReceivedDate { get; set; }
        public string ReceiveSource { get; set; }
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string ContactAddress { get; set; }
        public string AreaName { get; set; }
        public string IncidentAddress { get; set; }
        public string Content { get; set; }
        public string CaseType { get; set; }
        public string ContentGroup { get; set; }
        public string Field { get; set; }
        public string RelatedPerson { get; set; }
        public string ExpectedHandlingMethod { get; set; }
        public string SeverityLevel { get; set; }
        public string ExpectedResultDate { get; set; }
        public string PriorityLevel { get; set; }
        public string Note { get; set; }
        public string AdditionalNote { get; set; }
        public IReadOnlyList<AttachmentDraft> Attachments { get; set; } = new List<AttachmentDraft>();
    }

    public sealed class ProcessStep
    {
        public int StepNumber { get; set; }
        public string IconGlyph { get; set; }
        public string Title { get; set; }
        public string TimeText { get; set; }
        public bool IsDone { get; set; }
        public bool IsCurrent { get; set; }
    }

    public sealed class ProcessHistoryItem
    {
        public string Title { get; set; }
        public string ProcessedAt { get; set; }
        public string ProcessorName { get; set; }
        public string Content { get; set; }
        public bool IsCompleted { get; set; }
    }

    public sealed class ProcessingRecordDetail
    {
        public string RecordCode { get; set; }
        public string ReceivedDate { get; set; }
        public string ReceiveSource { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string AreaName { get; set; }
        public string CaseType { get; set; }
        public string Field { get; set; }
        public string Status { get; set; }
        public string ProcessorName { get; set; }
        public string ProcessingDate { get; set; }
        public string ProcessContent { get; set; }
        public string ProcessNote { get; set; }
        public IReadOnlyList<ProcessStep> Steps { get; set; } = new List<ProcessStep>();
        public IReadOnlyList<ProcessHistoryItem> History { get; set; } = new List<ProcessHistoryItem>();
    }

    public sealed class ExportRecordPreview
    {
        public int Index { get; set; }
        public string RecordCode { get; set; }
        public string ReceivedDate { get; set; }
        public string SenderName { get; set; }
        public string AreaName { get; set; }
        public string CaseType { get; set; }
        public string Field { get; set; }
        public string Status { get; set; }
    }
}
