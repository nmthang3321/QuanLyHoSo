using System;
using System.Collections.Generic;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ApplicationServices.Abstractions
{
    /// <summary>
    /// Presentation-facing data facade. The implementation preserves the same API
    /// whether data is served by local SQLite or by the LAN server.
    /// </summary>
    public interface IApplicationDataService
    {
        event Action<string> CatalogChanged;

        string DatabasePath { get; }

        void Initialize();
        IReadOnlyList<string> GetAreaNames(bool includeAll = false);
        IReadOnlyList<string> GetCatalogValues(string catalogType, bool includeAll = false);
        IReadOnlyList<CatalogValueSetting> GetCatalogItems(string catalogType, bool includeInactive = false);
        IReadOnlyDictionary<string, int> CountCatalogItemsByType();
        IReadOnlyList<SystemLogEntry> GetSystemLogs(int take = 200);
        AppUser AuthenticateUser(string userName, string password);
        IReadOnlyList<AppUser> GetUsers();
        bool SaveUser(AppUser user, string password);
        bool DeleteUser(int userId);
        bool ChangeCurrentUserPassword(string currentPassword, string newPassword);
        int AddCatalogItem(string catalogType, string name);
        bool UpdateCatalogItem(int id, string name);
        bool DeleteCatalogItem(int id);
        void UpdateCatalogItemOrders(IReadOnlyList<CatalogValueSetting> items);
        IReadOnlyList<string> GetProcessorNames(bool includeAll = false);
        IReadOnlyList<StaffPerformanceRow> GetStaffPerformanceRows(DateTime? fromDate = null, DateTime? toDate = null);
        IReadOnlyList<StatusStat> GetStaffDeadlineStats(DateTime? fromDate = null, DateTime? toDate = null);
        IReadOnlyList<StaffWorkRecord> GetStaffActiveRecords(string processorName, DateTime? fromDate = null, DateTime? toDate = null, int take = 4);
        (string Message, string ReceivedText) GetLatestLeadershipNotice(string officerName);
        StaffNotificationPage GetLeadershipNotices(string officerName, int skip, int take, bool adminOnly = false, bool includeAll = false);
        int CountUnreadLeadershipNotices(string officerName, bool adminOnly = false, bool includeAll = false);
        void MarkLeadershipNoticesAsRead(string officerName, IReadOnlyList<int> noticeIds, bool includeAll = false);
        string GetLatestLeadershipKpiTarget(string officerName);
        void SaveLeadershipKpi(string scope, string targetName, string kpiTarget);
        void SaveLeadershipNotice(string scope, string targetName, string kpiTarget, string message);
        int GetAreaCount();
        string GetNextRecordCode();
        SimilarRecordMatch FindSimilarRecord(RecordFormDraft record, int dateRangeDays = 30);
        IReadOnlyList<DashboardMetric> GetDashboardMetrics(DateTime? fromDate = null, DateTime? toDate = null, DateTime? previousFromDate = null, DateTime? previousToDate = null);
        IReadOnlyList<StatusStat> GetStatusStats(DateTime? fromDate = null, DateTime? toDate = null);
        IReadOnlyList<AreaStat> GetTopAreas(int take = 5, DateTime? fromDate = null, DateTime? toDate = null);
        IReadOnlyList<TrendStat> GetReceivedTrendStats(DateTime? fromDate = null, DateTime? toDate = null);
        IReadOnlyList<RecentRecord> GetRecentRecords(int take = 8, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0);
        IReadOnlyList<RecentRecord> GetFilteredRecords(DateTime? fromDate, DateTime? toDate, string status, string caseType, string field, string areaName, string processorName, string searchText, string sortOption, int take, int skip);
        RecordFormDraft GetLatestRecordForm();
        RecordFormDraft GetRecordForm(string recordCode);
        string SaveRecordForm(RecordFormDraft record, string originalRecordCode = null);
        bool DeleteRecord(string recordCode, string deletionBatchId = null);
        IReadOnlyList<DeletedRecord> GetDeletedRecords();
        bool PermanentlyDeleteRecord(string recordCode, string deletionBatchId);
        bool RestoreRecord(string recordCode, string deletionBatchId);
        ProcessingRecordDetail GetProcessingRecordDetail(string recordCode = null);
        void UpdateProcessingRecord(string recordCode, string status, DateTime processedAt, string processorName, string content, string note, string transferAreaName, IReadOnlyList<AttachmentDraft> attachments, bool generateInitialResultDocuments = false);
        IReadOnlyList<DashboardMetric> GetProcessingQueueMetrics();
        IReadOnlyList<ProcessingQueueRecord> GetProcessingQueueRecords(string searchText, string status, string areaName, string severityLevel, string cardFilterKey, int skip = 0, int take = 20);
        int CountProcessingQueueRecords(string searchText, string status, string areaName, string severityLevel, string cardFilterKey);
        IReadOnlyList<ExportRecordPreview> GetExportPreview(DateTime? fromDate, DateTime? toDate, string status, string caseType, string field, string areaName, string processorName, string searchText, string sortOption, int take = 5000);
        int CountExportRecords(DateTime? fromDate, DateTime? toDate, string status, string caseType, string field, string areaName, string processorName, string searchText);
        int CountFilteredRecords(DateTime? fromDate, DateTime? toDate, string status, string caseType, string field, string areaName, string processorName, string searchText);
        int CountRecords(DateTime? fromDate = null, DateTime? toDate = null);
        void BackupDatabase(string destinationPath);
        string CreateBackupFile(string fileName);
        void DownloadBackupFile(string fileName, string destinationPath);
        string RestoreDatabaseFromUpload(string fileName, byte[] content);
        InternalUpdatePackageInfo GetInternalUpdatePackageInfo();
        void DownloadInternalUpdatePackage(string fileName, string destinationPath);
    }
}
