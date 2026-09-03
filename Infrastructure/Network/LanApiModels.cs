using System;
using System.Collections.Generic;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Network
{
    public sealed class LanApiEnvelope<T>
    {
        public AppUser User { get; set; }
        public T Data { get; set; }
    }

    public sealed class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class IncludeAllRequest
    {
        public bool IncludeAll { get; set; }
    }

    public sealed class CatalogValuesRequest : IncludeAllRequest
    {
        public string CatalogType { get; set; }
    }

    public sealed class DashboardMetricsRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? PreviousFromDate { get; set; }
        public DateTime? PreviousToDate { get; set; }
    }

    public class DateRangeRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public sealed class TopAreasRequest : DateRangeRequest
    {
        public int Take { get; set; }
    }

    public sealed class RecentRecordsRequest : DateRangeRequest
    {
        public int Take { get; set; }
        public int Skip { get; set; }
    }

    public sealed class FilteredRecordsRequest : DateRangeRequest
    {
        public string Status { get; set; }
        public string CaseType { get; set; }
        public string Field { get; set; }
        public string AreaName { get; set; }
        public string ProcessorName { get; set; }
        public string SearchText { get; set; }
        public string SortOption { get; set; }
        public int Take { get; set; }
        public int Skip { get; set; }
    }

    public sealed class RecordCodeRequest
    {
        public string RecordCode { get; set; }
    }

    public sealed class ProcessingQueueRequest
    {
        public string SearchText { get; set; }
        public string Status { get; set; }
        public string AreaName { get; set; }
        public string SeverityLevel { get; set; }
        public string CardFilterKey { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }

    public sealed class UpdateProcessingRequest
    {
        public string RecordCode { get; set; }
        public string Status { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ProcessorName { get; set; }
        public string Content { get; set; }
        public string Note { get; set; }
        public string TransferAreaName { get; set; }
        public IReadOnlyList<AttachmentDraft> Attachments { get; set; }
        public bool GenerateInitialResultDocuments { get; set; }
    }

    public sealed class StaffActiveRecordsRequest : DateRangeRequest
    {
        public string ProcessorName { get; set; }
        public int Take { get; set; }
    }

    public sealed class LeadershipNoticeRequest
    {
        public string OfficerName { get; set; }
    }

    public sealed class LeadershipNoticeResponse
    {
        public string Message { get; set; }
        public string ReceivedText { get; set; }
    }

    public sealed class SaveLeadershipNoticeRequest
    {
        public string Scope { get; set; }
        public string TargetName { get; set; }
        public string KpiTarget { get; set; }
        public string Message { get; set; }
    }
}
