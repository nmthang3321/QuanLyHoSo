using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Infrastructure.Data
{
    public sealed class AppDataService
    {
        private static readonly Lazy<AppDataService> LazyInstance = new Lazy<AppDataService>(() => new AppDataService());
        private readonly string _connectionString;

        private AppDataService()
        {
            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Data");

            Directory.CreateDirectory(dataFolder);
            DatabasePath = Path.Combine(dataFolder, "quanlyhoso.db");
            _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        }

        public static AppDataService Instance => LazyInstance.Value;

        public string DatabasePath { get; }

        public void Initialize()
        {
            AppLogger.Info("Database", "Initialize", $"Initializing database at {DatabasePath}.");
            using var connection = OpenConnection();
            CreateSchema(connection);
            SeedAreas(connection);
            SeedCatalogs(connection);
            SeedRecords(connection);
        }

        public IReadOnlyList<string> GetAreaNames(bool includeAll = false)
        {
            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AreaType, Name FROM Areas ORDER BY DisplayOrder;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add($"{reader.GetString(0)} {reader.GetString(1)}");
            }

            return result;
        }

        public IReadOnlyList<string> GetCatalogValues(string catalogType, bool includeAll = false)
        {
            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Name
FROM CatalogItems
WHERE CatalogType = $catalogType AND IsActive = 1
ORDER BY DisplayOrder;";
            command.Parameters.AddWithValue("$catalogType", catalogType);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        public IReadOnlyList<CatalogValueSetting> GetCatalogItems(string catalogType)
        {
            var result = new List<CatalogValueSetting>();

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, CatalogType, Name, DisplayOrder
FROM CatalogItems
WHERE CatalogType = $catalogType AND IsActive = 1
ORDER BY DisplayOrder, Name;";
            command.Parameters.AddWithValue("$catalogType", catalogType);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CatalogValueSetting
                {
                    Id = reader.GetInt32(0),
                    CatalogType = reader.GetString(1),
                    Name = reader.GetString(2),
                    DisplayOrder = reader.GetInt32(3)
                });
            }

            return result;
        }

        public int AddCatalogItem(string catalogType, string name)
        {
            if (string.IsNullOrWhiteSpace(catalogType) || string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            using var connection = OpenConnection();
            var trimmedName = name.Trim();
            if (CatalogNameExists(connection, catalogType, trimmedName))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive)
VALUES (
    $catalogType,
    $name,
    COALESCE((SELECT MAX(DisplayOrder) + 1 FROM CatalogItems WHERE CatalogType = $catalogType), 1),
    1);
SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            command.Parameters.AddWithValue("$name", trimmedName);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public bool UpdateCatalogItem(int id, string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            using var connection = OpenConnection();
            var trimmedName = name.Trim();
            var catalogType = GetCatalogType(connection, id);
            if (string.IsNullOrWhiteSpace(catalogType) || CatalogNameExists(connection, catalogType, trimmedName, id))
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE CatalogItems SET Name = $name WHERE Id = $id AND IsActive = 1;";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$name", trimmedName);
            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteCatalogItem(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE CatalogItems SET IsActive = 0 WHERE Id = $id AND IsActive = 1;";
            command.Parameters.AddWithValue("$id", id);
            return command.ExecuteNonQuery() > 0;
        }

        public void UpdateCatalogItemOrders(IReadOnlyList<CatalogValueSetting> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            for (var index = 0; index < items.Count; index++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "UPDATE CatalogItems SET DisplayOrder = $displayOrder WHERE Id = $id;";
                command.Parameters.AddWithValue("$displayOrder", index + 1);
                command.Parameters.AddWithValue("$id", items[index].Id);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public IReadOnlyList<string> GetProcessorNames(bool includeAll = false)
        {
            var result = new List<string>();
            if (includeAll)
            {
                result.Add("Tất cả");
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT ProcessorName FROM Records ORDER BY ProcessorName;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        public int GetAreaCount()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Areas;";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public IReadOnlyList<DashboardMetric> GetDashboardMetrics(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var connection = OpenConnection();
            var total = CountRecords(connection, fromDate, toDate);
            var processing = CountRecordsByStatuses(connection, fromDate, toDate, "Đang phân loại", "Đã phân công", "Đang xác minh", "Đang xử lý");
            var resolved = CountRecordsByStatuses(connection, fromDate, toDate, "Đã giải quyết");
            var waiting = CountRecordsByStatuses(connection, fromDate, toDate, "Chờ kết quả", "Đang chờ bổ sung tài liệu");

            return new List<DashboardMetric>
            {
                new DashboardMetric { Title = "TỔNG HỒ SƠ", Value = total.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Dữ liệu từ SQLite", IconGlyph = "\uE8A5", AccentColor = "#0B5CFF" },
                new DashboardMetric { Title = "ĐANG XỬ LÝ", Value = processing.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Theo trạng thái đang mở", IconGlyph = "\uE823", AccentColor = "#F28C18" },
                new DashboardMetric { Title = "ĐÃ GIẢI QUYẾT", Value = resolved.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Hồ sơ hoàn tất", IconGlyph = "\uE73E", AccentColor = "#1FA24A" },
                new DashboardMetric { Title = "CHỜ KẾT QUẢ", Value = waiting.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Hồ sơ đang chờ", IconGlyph = "\uE916", AccentColor = "#7147D8" }
            };
        }

        public IReadOnlyList<StatusStat> GetStatusStats(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var connection = OpenConnection();
            var total = Math.Max(CountRecords(connection, fromDate, toDate), 1);
            var result = new List<StatusStat>();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT Status, COUNT(*)
FROM Records
{BuildDateWhere(fromDate, toDate)}
GROUP BY Status
ORDER BY COUNT(*) DESC, Status;";
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var status = reader.GetString(0);
                var count = reader.GetInt32(1);
                result.Add(new StatusStat
                {
                    Name = status,
                    Count = count,
                    Percentage = $"{count * 100.0 / total:0.0}%",
                    Width = Math.Max(8, (int)Math.Round(count * 130.0 / total)),
                    Color = GetStatusColor(status)
                });
            }

            var startAngle = -90.0;
            foreach (var item in result)
            {
                item.StartAngle = startAngle;
                item.SweepAngle = item.Count * 360.0 / total;
                startAngle += item.SweepAngle;
            }

            return result;
        }

        public IReadOnlyList<AreaStat> GetTopAreas(int take = 5, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var connection = OpenConnection();
            var rows = new List<(string AreaName, int Count)>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT AreaName, COUNT(*) AS Total
FROM Records
{BuildDateWhere(fromDate, toDate)}
GROUP BY AreaName
ORDER BY Total DESC, AreaName
LIMIT $take;";
            command.Parameters.AddWithValue("$take", take);
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((reader.GetString(0), reader.GetInt32(1)));
            }

            var max = rows.Count == 0 ? 1 : Math.Max(1, rows[0].Count);
            var result = new List<AreaStat>();
            foreach (var row in rows)
            {
                result.Add(new AreaStat
                {
                    AreaName = row.AreaName,
                    Count = row.Count,
                    Width = Math.Max(36, (int)Math.Round(row.Count * 270.0 / max))
                });
            }

            return result;
        }

        public IReadOnlyList<RecentRecord> GetRecentRecords(int take = 8, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0)
        {
            using var connection = OpenConnection();
            var result = new List<RecentRecord>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT RecordCode, SenderName, AreaName, Status, UpdatedAt, ProcessorName
FROM Records
{BuildDateWhere(fromDate, toDate)}
ORDER BY UpdatedAt DESC
LIMIT $take OFFSET $skip;";
            command.Parameters.AddWithValue("$take", take);
            command.Parameters.AddWithValue("$skip", skip);
            AddDateParameters(command, fromDate, toDate);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RecentRecord
                {
                    RecordCode = reader.GetString(0),
                    SenderName = reader.GetString(1),
                    AreaName = reader.GetString(2),
                    Status = reader.GetString(3),
                    UpdatedAt = FormatDateTime(reader.GetString(4)),
                    ProcessorName = reader.GetString(5)
                });
            }

            return result;
        }

        public RecordFormDraft GetLatestRecordForm()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
       AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
       ExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, PriorityLevel, Note, AdditionalNote
FROM Records
ORDER BY UpdatedAt DESC
LIMIT 1;";
            return ReadRecordForm(connection, command);
        }

        public RecordFormDraft GetRecordForm(string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return new RecordFormDraft();
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
       AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
       ExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, PriorityLevel, Note, AdditionalNote
FROM Records
WHERE RecordCode = $recordCode
LIMIT 1;";
            command.Parameters.AddWithValue("$recordCode", recordCode);
            return ReadRecordForm(connection, command);
        }

        public void SaveRecordForm(RecordFormDraft record, string originalRecordCode = null)
        {
            if (record == null)
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var lookupCode = string.IsNullOrWhiteSpace(originalRecordCode) ? record.RecordCode : originalRecordCode;
            var recordId = GetRecordId(connection, transaction, lookupCode);
            var now = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);

            if (recordId.HasValue)
            {
                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
UPDATE Records
SET RecordCode = $recordCode,
    ReceivedDate = $receivedDate,
    ReceiveSource = $receiveSource,
    ReceiverName = $receiverName,
    SenderName = $senderName,
    SenderPhone = $senderPhone,
    ContactAddress = $contactAddress,
    AreaName = $areaName,
    IncidentAddress = $incidentAddress,
    Content = $content,
    CaseType = $caseType,
    ContentGroup = $contentGroup,
    Field = $field,
    RelatedPerson = $relatedPerson,
    ExpectedHandlingMethod = $method,
    SeverityLevel = $severity,
    ExpectedResultDate = $expectedDate,
    PriorityLevel = $priority,
    Note = $note,
    AdditionalNote = $additionalNote,
    UpdatedAt = $updatedAt
WHERE Id = $recordId;";
                updateCommand.Parameters.AddWithValue("$recordId", recordId.Value);
                AddRecordFormParameters(updateCommand, record, now);
                updateCommand.ExecuteNonQuery();
                ReplaceAttachments(connection, transaction, recordId.Value, record.Attachments);
            }
            else
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
INSERT INTO Records (
    RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, PriorityLevel, Status, ProcessorName,
    Note, AdditionalNote, CreatedAt, UpdatedAt)
VALUES (
    $recordCode, $receivedDate, $receiveSource, $receiverName, $senderName, $senderPhone, $contactAddress,
    $areaName, $incidentAddress, $content, $caseType, $contentGroup, $field, $relatedPerson,
    $method, $severity, $expectedDate, $priority, $status, $processor,
    $note, $additionalNote, $createdAt, $updatedAt);
SELECT last_insert_rowid();";
                AddRecordFormParameters(insertCommand, record, now);
                insertCommand.Parameters.AddWithValue("$status", "Mới tiếp nhận");
                insertCommand.Parameters.AddWithValue("$processor", NormalizeDbText(record.ReceiverName));
                insertCommand.Parameters.AddWithValue("$createdAt", now);
                var insertedId = Convert.ToInt32(insertCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                ReplaceAttachments(connection, transaction, insertedId, record.Attachments);
            }

            transaction.Commit();
        }

        public bool DeleteRecord(string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return false;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = "SELECT Id FROM Records WHERE RecordCode = $recordCode LIMIT 1;";
            selectCommand.Parameters.AddWithValue("$recordCode", recordCode);

            var recordIdValue = selectCommand.ExecuteScalar();
            if (recordIdValue == null)
            {
                transaction.Commit();
                return false;
            }

            var recordId = Convert.ToInt32(recordIdValue, CultureInfo.InvariantCulture);
            using var deleteHistoriesCommand = connection.CreateCommand();
            deleteHistoriesCommand.Transaction = transaction;
            deleteHistoriesCommand.CommandText = "DELETE FROM ProcessHistories WHERE RecordId = $recordId;";
            deleteHistoriesCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteHistoriesCommand.ExecuteNonQuery();

            using var deleteAttachmentsCommand = connection.CreateCommand();
            deleteAttachmentsCommand.Transaction = transaction;
            deleteAttachmentsCommand.CommandText = "DELETE FROM RecordAttachments WHERE RecordId = $recordId;";
            deleteAttachmentsCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteAttachmentsCommand.ExecuteNonQuery();

            using var deleteRecordCommand = connection.CreateCommand();
            deleteRecordCommand.Transaction = transaction;
            deleteRecordCommand.CommandText = "DELETE FROM Records WHERE Id = $recordId;";
            deleteRecordCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteRecordCommand.ExecuteNonQuery();

            transaction.Commit();
            return true;
        }

        private static int? GetRecordId(SqliteConnection connection, SqliteTransaction transaction, string recordCode)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return null;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT Id FROM Records WHERE RecordCode = $recordCode LIMIT 1;";
            command.Parameters.AddWithValue("$recordCode", recordCode);
            var value = command.ExecuteScalar();
            return value == null ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static void AddRecordFormParameters(SqliteCommand command, RecordFormDraft record, string updatedAt)
        {
            command.Parameters.AddWithValue("$recordCode", NormalizeDbText(record.RecordCode));
            command.Parameters.AddWithValue("$receivedDate", ParseDisplayDate(record.ReceivedDate).ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$receiveSource", NormalizeDbText(record.ReceiveSource));
            command.Parameters.AddWithValue("$receiverName", NormalizeDbText(record.ReceiverName));
            command.Parameters.AddWithValue("$senderName", NormalizeDbText(record.SenderName));
            command.Parameters.AddWithValue("$senderPhone", NormalizeDbText(record.SenderPhone));
            command.Parameters.AddWithValue("$contactAddress", NormalizeDbText(record.ContactAddress));
            command.Parameters.AddWithValue("$areaName", NormalizeDbText(record.AreaName));
            command.Parameters.AddWithValue("$incidentAddress", NormalizeDbText(record.IncidentAddress));
            command.Parameters.AddWithValue("$content", NormalizeDbText(record.Content));
            command.Parameters.AddWithValue("$caseType", NormalizeDbText(record.CaseType));
            command.Parameters.AddWithValue("$contentGroup", NormalizeDbText(record.ContentGroup));
            command.Parameters.AddWithValue("$field", NormalizeDbText(record.Field));
            command.Parameters.AddWithValue("$relatedPerson", NormalizeDbText(record.RelatedPerson));
            command.Parameters.AddWithValue("$method", NormalizeDbText(record.ExpectedHandlingMethod));
            command.Parameters.AddWithValue("$severity", NormalizeDbText(record.SeverityLevel));
            command.Parameters.AddWithValue("$expectedDate", string.IsNullOrWhiteSpace(record.ExpectedResultDate)
                ? string.Empty
                : ParseDisplayDate(record.ExpectedResultDate).ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$priority", NormalizeDbText(record.PriorityLevel));
            command.Parameters.AddWithValue("$note", NormalizeDbText(record.Note));
            command.Parameters.AddWithValue("$additionalNote", NormalizeDbText(record.AdditionalNote));
            command.Parameters.AddWithValue("$updatedAt", updatedAt);
        }

        private static void ReplaceAttachments(SqliteConnection connection, SqliteTransaction transaction, int recordId, IReadOnlyList<AttachmentDraft> attachments)
        {
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM RecordAttachments WHERE RecordId = $recordId;";
            deleteCommand.Parameters.AddWithValue("$recordId", recordId);
            deleteCommand.ExecuteNonQuery();

            if (attachments == null)
            {
                return;
            }

            foreach (var attachment in attachments)
            {
                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = "INSERT INTO RecordAttachments (RecordId, FileName, FileSize, FilePath) VALUES ($recordId, $fileName, $fileSize, $filePath);";
                insertCommand.Parameters.AddWithValue("$recordId", recordId);
                insertCommand.Parameters.AddWithValue("$fileName", NormalizeDbText(attachment.FileName));
                insertCommand.Parameters.AddWithValue("$fileSize", NormalizeDbText(attachment.FileSize));
                insertCommand.Parameters.AddWithValue("$filePath", NormalizeDbText(attachment.FilePath));
                insertCommand.ExecuteNonQuery();
            }
        }

        private static string NormalizeDbText(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private RecordFormDraft ReadRecordForm(SqliteConnection connection, SqliteCommand command)
        {

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new RecordFormDraft();
            }

            var recordId = reader.GetInt32(0);
            return new RecordFormDraft
            {
                RecordCode = reader.GetString(1),
                ReceivedDate = FormatDate(reader.GetString(2)),
                ReceiveSource = reader.GetString(3),
                ReceiverName = reader.GetString(4),
                SenderName = reader.GetString(5),
                SenderPhone = reader.GetString(6),
                ContactAddress = reader.GetString(7),
                AreaName = reader.GetString(8),
                IncidentAddress = reader.GetString(9),
                Content = reader.GetString(10),
                CaseType = reader.GetString(11),
                ContentGroup = reader.GetString(12),
                Field = reader.GetString(13),
                RelatedPerson = reader.GetString(14),
                ExpectedHandlingMethod = reader.GetString(15),
                SeverityLevel = reader.GetString(16),
                ExpectedResultDate = FormatDate(reader.GetString(17)),
                PriorityLevel = reader.GetString(18),
                Note = reader.GetString(19),
                AdditionalNote = reader.GetString(20),
                Attachments = GetAttachments(connection, recordId)
            };
        }

        public ProcessingRecordDetail GetProcessingRecordDetail(string recordCode = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var whereClause = string.IsNullOrWhiteSpace(recordCode)
                ? "WHERE Status <> 'Đã giải quyết'"
                : "WHERE RecordCode = $recordCode";
            command.CommandText = $@"
SELECT Id, RecordCode, ReceivedDate, ReceiveSource, SenderName, SenderPhone, AreaName, CaseType, Field, Status, ProcessorName, UpdatedAt
FROM Records
{whereClause}
ORDER BY UpdatedAt DESC
LIMIT 1;";
            if (!string.IsNullOrWhiteSpace(recordCode))
            {
                command.Parameters.AddWithValue("$recordCode", recordCode);
            }

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new ProcessingRecordDetail();
            }

            var recordId = reader.GetInt32(0);
            var status = reader.GetString(9);
            var history = GetProcessHistory(connection, recordId, status);
            return new ProcessingRecordDetail
            {
                RecordCode = reader.GetString(1),
                ReceivedDate = FormatDate(reader.GetString(2)),
                ReceiveSource = reader.GetString(3),
                SenderName = reader.GetString(4),
                SenderPhone = reader.GetString(5),
                AreaName = reader.GetString(6),
                CaseType = reader.GetString(7),
                Field = reader.GetString(8),
                Status = status,
                ProcessorName = reader.GetString(10),
                ProcessingDate = FormatDateTime(reader.GetString(11)),
                ProcessContent = "Đang cập nhật tiến độ xử lý hồ sơ theo thông tin từ cơ sở dữ liệu.",
                ProcessNote = "Dữ liệu mẫu được seed tự động cho giai đoạn thiết kế giao diện.",
                Steps = BuildProcessSteps(status, history),
                History = history
            };
        }

        public void UpdateProcessingRecord(string recordCode, string status, DateTime processedAt, string processorName, string content, string note)
        {
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                return;
            }

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var recordId = GetRecordId(connection, transaction, recordCode);
            if (!recordId.HasValue)
            {
                transaction.Commit();
                return;
            }

            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = @"
UPDATE Records
SET Status = $status,
    ProcessorName = $processor,
    Note = CASE WHEN $note = '' THEN Note ELSE $note END,
    UpdatedAt = $updatedAt
WHERE Id = $recordId;";
            updateCommand.Parameters.AddWithValue("$recordId", recordId.Value);
            updateCommand.Parameters.AddWithValue("$status", NormalizeDbText(status));
            updateCommand.Parameters.AddWithValue("$processor", NormalizeDbText(processorName));
            updateCommand.Parameters.AddWithValue("$note", NormalizeDbText(note));
            updateCommand.Parameters.AddWithValue("$updatedAt", processedAt.ToString("O", CultureInfo.InvariantCulture));
            updateCommand.ExecuteNonQuery();

            var currentStep = GetProcessStepNumber(status);
            DeleteProcessHistoryFromStep(connection, transaction, recordId.Value, currentStep);

            for (var step = 1; step <= currentStep; step++)
            {
                var definition = GetProcessStepDefinition(step);
                var stepContent = step == currentStep
                    ? NormalizeDbText(content)
                    : $"Tự động ghi nhận bước {definition.Title.ToLower(CultureInfo.GetCultureInfo("vi-VN"))} khi hồ sơ được chuyển đến bước {status}.";
                var isCompleted = step < currentStep || currentStep >= 6;

                if (step < currentStep && HasProcessHistory(connection, transaction, recordId.Value, definition.Title))
                {
                    continue;
                }

                InsertProcessHistory(
                    connection,
                    transaction,
                    recordId.Value,
                    definition.Title,
                    processedAt,
                    NormalizeDbText(processorName),
                    stepContent,
                    isCompleted);
            }

            transaction.Commit();
        }

        public IReadOnlyList<DashboardMetric> GetProcessingQueueMetrics()
        {
            using var connection = OpenConnection();
            var needClassify = CountRecordsByStatuses(connection, null, null, "Mới tiếp nhận", "Đang phân loại");
            var processing = CountRecordsByStatuses(connection, null, null, "Đã phân công", "Đang xác minh");
            var waiting = CountRecordsByStatuses(connection, null, null, "Chờ kết quả", "Đang chờ bổ sung tài liệu");
            var overdue = CountOverdueOpenRecords(connection);

            return new List<DashboardMetric>
            {
                new DashboardMetric { Title = "CẦN PHÂN LOẠI", Value = needClassify.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Hồ sơ mới/chưa phân loại", IconGlyph = "\uE8F1", AccentColor = "#0B5CFF" },
                new DashboardMetric { Title = "ĐANG XỬ LÝ", Value = processing.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Đã phân công, xác minh", IconGlyph = "\uE823", AccentColor = "#F28C18" },
                new DashboardMetric { Title = "CHỜ BỔ SUNG", Value = waiting.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Đang chờ tài liệu/kết quả", IconGlyph = "\uE916", AccentColor = "#7147D8" },
                new DashboardMetric { Title = "QUÁ HẠN", Value = overdue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), Delta = "Chưa hoàn tất theo hẹn", IconGlyph = "\uE7BA", AccentColor = "#D13438" }
            };
        }

        public IReadOnlyList<ProcessingQueueRecord> GetProcessingQueueRecords(
            string searchText = null,
            string status = null,
            string areaName = null,
            string priorityLevel = null,
            int take = 20)
        {
            using var connection = OpenConnection();
            var result = new List<ProcessingQueueRecord>();
            using var command = connection.CreateCommand();
            var conditions = new List<string> { "Status <> 'Đã giải quyết'" };

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(RecordCode LIKE $search OR SenderName LIKE $search OR Content LIKE $search)");
                command.Parameters.AddWithValue("$search", $"%{searchText.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                conditions.Add("Status = $status");
                command.Parameters.AddWithValue("$status", status);
            }

            if (!string.IsNullOrWhiteSpace(areaName) && areaName != "Tất cả")
            {
                conditions.Add("AreaName = $areaName");
                command.Parameters.AddWithValue("$areaName", areaName);
            }

            if (!string.IsNullOrWhiteSpace(priorityLevel) && priorityLevel != "Tất cả")
            {
                conditions.Add("PriorityLevel = $priorityLevel");
                command.Parameters.AddWithValue("$priorityLevel", priorityLevel);
            }

            command.CommandText = $@"
SELECT RecordCode, ReceivedDate, SenderName, AreaName, CaseType, PriorityLevel, Status, UpdatedAt
FROM Records
WHERE {string.Join(" AND ", conditions)}
ORDER BY UpdatedAt DESC
LIMIT $take;";
            command.Parameters.AddWithValue("$take", Math.Max(1, Math.Min(20, take)));

            using var reader = command.ExecuteReader();
            var index = 1;
            while (reader.Read())
            {
                result.Add(new ProcessingQueueRecord
                {
                    Index = index++,
                    RecordCode = reader.GetString(0),
                    ReceivedDate = FormatDate(reader.GetString(1)),
                    SenderName = reader.GetString(2),
                    AreaName = reader.GetString(3),
                    CaseType = reader.GetString(4),
                    PriorityLevel = reader.GetString(5),
                    Status = reader.GetString(6),
                    UpdatedAt = FormatDateTime(reader.GetString(7))
                });
            }

            return result;
        }

        public IReadOnlyList<ExportRecordPreview> GetExportPreview(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null,
            string sortOption = null,
            int take = 50)
        {
            using var connection = OpenConnection();
            var result = new List<ExportRecordPreview>();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText);
            command.CommandText = $@"
SELECT RecordCode, ReceivedDate, SenderName, AreaName, CaseType, Field, Status
FROM Records
{whereClause}
ORDER BY {BuildExportOrderBy(sortOption)}
LIMIT $take;";
            command.Parameters.AddWithValue("$take", Math.Max(1, take));

            using var reader = command.ExecuteReader();
            var index = 1;
            while (reader.Read())
            {
                result.Add(new ExportRecordPreview
                {
                    Index = index++,
                    RecordCode = reader.GetString(0),
                    ReceivedDate = FormatDate(reader.GetString(1)),
                    SenderName = reader.GetString(2),
                    AreaName = reader.GetString(3),
                    CaseType = reader.GetString(4),
                    Field = reader.GetString(5),
                    Status = reader.GetString(6)
                });
            }

            return result;
        }

        public int CountExportRecords(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            string caseType = null,
            string field = null,
            string areaName = null,
            string processorName = null,
            string searchText = null)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            var whereClause = BuildExportWhere(command, fromDate, toDate, status, caseType, field, areaName, processorName, searchText);
            command.CommandText = $"SELECT COUNT(*) FROM Records {whereClause};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        public int CountRecords(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var connection = OpenConnection();
            return CountRecords(connection, fromDate, toDate);
        }

        private SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static void CreateSchema(SqliteConnection connection)
        {
            ExecuteNonQuery(connection, @"
CREATE TABLE IF NOT EXISTS Areas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    AreaType TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    IsArranged INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS CatalogItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CatalogType TEXT NOT NULL,
    Name TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    IsActive INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Records (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordCode TEXT NOT NULL UNIQUE,
    ReceivedDate TEXT NOT NULL,
    ReceiveSource TEXT NOT NULL,
    ReceiverName TEXT NOT NULL,
    SenderName TEXT NOT NULL,
    SenderPhone TEXT NOT NULL,
    ContactAddress TEXT NOT NULL,
    AreaName TEXT NOT NULL,
    IncidentAddress TEXT NOT NULL,
    Content TEXT NOT NULL,
    CaseType TEXT NOT NULL,
    ContentGroup TEXT NOT NULL,
    Field TEXT NOT NULL,
    RelatedPerson TEXT NOT NULL,
    ExpectedHandlingMethod TEXT NOT NULL,
    SeverityLevel TEXT NOT NULL,
    ExpectedResultDate TEXT NOT NULL,
    PriorityLevel TEXT NOT NULL,
    Status TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Note TEXT NOT NULL,
    AdditionalNote TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS RecordAttachments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FileSize TEXT NOT NULL,
    FilePath TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);

CREATE TABLE IF NOT EXISTS ProcessHistories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    ProcessedAt TEXT NOT NULL,
    ProcessorName TEXT NOT NULL,
    Content TEXT NOT NULL,
    IsCompleted INTEGER NOT NULL,
    FOREIGN KEY (RecordId) REFERENCES Records(Id)
);");
            TryAddColumn(connection, "RecordAttachments", "FilePath", "TEXT NOT NULL DEFAULT ''");
        }

        private static void SeedAreas(SqliteConnection connection)
        {
            if (CountRows(connection, "Areas") > 0)
            {
                return;
            }

            var areas = new List<(string Type, string Name, bool IsArranged)>();
            var arrangedCommunes = new[]
            {
                "An Phú","Vĩnh Hậu","Nhơn Hội","Khánh Bình","Phú Hữu","Tân An","Châu Phong","Vĩnh Xương","Phú Tân","Phú An",
                "Bình Thạnh Đông","Chợ Vàm","Hòa Lạc","Phú Lâm","Châu Phú","Mỹ Đức","Vĩnh Thạnh Trung","Bình Mỹ","Thạnh Mỹ Tây","An Cư",
                "Núi Cấm","Ba Chúc","Tri Tôn","Ô Lâm","Cô Tô","Vĩnh Gia","An Châu","Bình Hòa","Cần Đăng","Vĩnh Hanh",
                "Vĩnh An","Chợ Mới","Cù Lao Giêng","Hội An","Long Điền","Nhơn Mỹ","Long Kiến","Thoại Sơn","Óc Eo","Định Mỹ",
                "Phú Hòa","Vĩnh Trạch","Tây Phú","Vĩnh Bình","Vĩnh Thuận","Vĩnh Phong","Vĩnh Hòa","U Minh Thượng","Đông Hòa","Tân Thạnh",
                "Đông Hưng","An Minh","Vân Khánh","Tây Yên","Đông Thái","An Biên","Định Hòa","Gò Quao","Vĩnh Hòa Hưng","Vĩnh Tuy",
                "Giồng Riềng","Thạnh Hưng","Long Thạnh","Hòa Hưng","Ngọc Chúc","Hòa Thuận","Tân Hội","Tân Hiệp","Thạnh Đông","Thạnh Lộc",
                "Châu Thành","Bình An","Hòn Đất","Sơn Kiên","Mỹ Thuận","Hòa Điền","Kiên Lương","Giang Thành","Vĩnh Điều"
            };
            foreach (var name in arrangedCommunes)
            {
                areas.Add(("Xã", name, true));
            }

            foreach (var name in new[] { "Mỹ Hòa Hưng", "Bình Giang", "Bình Sơn", "Hòn Nghệ", "Sơn Hải", "Tiên Hải" })
            {
                areas.Add(("Xã", name, false));
            }

            foreach (var name in new[] { "Long Xuyên", "Bình Đức", "Mỹ Thới", "Châu Đốc", "Vĩnh Tế", "Tân Châu", "Long Phú", "Tịnh Biên", "Thới Sơn", "Chi Lăng", "Vĩnh Thông", "Rạch Giá", "Hà Tiên", "Tô Châu" })
            {
                areas.Add(("Phường", name, true));
            }

            foreach (var name in new[] { "Kiên Hải", "Phú Quốc", "Thổ Châu" })
            {
                areas.Add(("Đặc khu", name, true));
            }

            using var transaction = connection.BeginTransaction();
            for (var i = 0; i < areas.Count; i++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Areas (Name, AreaType, DisplayOrder, IsArranged) VALUES ($name, $type, $displayOrder, $isArranged);";
                command.Parameters.AddWithValue("$name", areas[i].Name);
                command.Parameters.AddWithValue("$type", areas[i].Type);
                command.Parameters.AddWithValue("$displayOrder", i + 1);
                command.Parameters.AddWithValue("$isArranged", areas[i].IsArranged ? 1 : 0);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void SeedCatalogs(SqliteConnection connection)
        {
            if (CountRows(connection, "CatalogItems") > 0)
            {
                return;
            }

            var catalogs = new List<(string Type, string Name)>
            {
                ("ReceiveSource", "Trực tiếp"),
                ("ReceiveSource", "Qua bưu điện"),
                ("ReceiveSource", "Cổng thông tin"),
                ("ReceiveSource", "Cơ quan chuyển đến"),
                ("CaseType", "Khiếu nại"),
                ("CaseType", "Tố cáo"),
                ("CaseType", "Kiến nghị"),
                ("CaseType", "Phản ánh"),
                ("Field", "Quản lý đất đai"),
                ("Field", "Xây dựng"),
                ("Field", "Tài nguyên môi trường"),
                ("Field", "Trật tự đô thị"),
                ("ContentGroup", "Đất đai - Xây dựng"),
                ("ContentGroup", "Môi trường"),
                ("ContentGroup", "An ninh trật tự"),
                ("ContentGroup", "Khác"),
                ("Priority", "Bình thường"),
                ("Priority", "Ưu tiên"),
                ("Priority", "Khẩn"),
                ("ExpectedHandlingMethod", "Đề nghị kiểm tra, xử lý"),
                ("ExpectedHandlingMethod", "Chuyển cơ quan có thẩm quyền"),
                ("ExpectedHandlingMethod", "Theo dõi, tổng hợp")
            };

            using var transaction = connection.BeginTransaction();
            var orderByType = new Dictionary<string, int>();
            foreach (var catalog in catalogs)
            {
                orderByType.TryGetValue(catalog.Type, out var order);
                orderByType[catalog.Type] = ++order;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO CatalogItems (CatalogType, Name, DisplayOrder, IsActive) VALUES ($type, $name, $displayOrder, 1);";
                command.Parameters.AddWithValue("$type", catalog.Type);
                command.Parameters.AddWithValue("$name", catalog.Name);
                command.Parameters.AddWithValue("$displayOrder", order);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void SeedRecords(SqliteConnection connection)
        {
            if (CountRows(connection, "Records") > 0)
            {
                return;
            }

            var random = new Random();
            var areaNames = ReadAreaDisplayNames(connection);
            var receiveSources = ReadCatalog(connection, "ReceiveSource");
            var caseTypes = ReadCatalog(connection, "CaseType");
            var fields = ReadCatalog(connection, "Field");
            var contentGroups = ReadCatalog(connection, "ContentGroup");
            var priorities = ReadCatalog(connection, "Priority");
            var methods = ReadCatalog(connection, "ExpectedHandlingMethod");
            var statuses = new[] { "Mới tiếp nhận", "Đang phân loại", "Đã phân công", "Đang xác minh", "Chờ kết quả", "Đang chờ bổ sung tài liệu", "Đã giải quyết", "Chuyển cơ quan khác" };
            var processors = new[] { "Trần Văn B", "Trần Văn C", "Lê Thị D", "Nguyễn Thị H", "Phạm Văn K" };
            var senders = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D", "Võ Văn E", "Huỳnh Văn F", "Đặng Thị G", "Bùi Văn H" };
            var relatedPersons = new[] { "Trần Văn C", "Lê Thị D", "Nguyễn Văn M", "Công ty TNHH An Phú", "Hộ dân liền kề" };

            using var transaction = connection.BeginTransaction();
            for (var i = 1; i <= 50; i++)
            {
                var receivedDate = DateTime.Today.AddDays(-random.Next(0, 90)).AddHours(random.Next(8, 17)).AddMinutes(random.Next(0, 60));
                var updatedAt = receivedDate.AddDays(random.Next(0, 12)).AddHours(random.Next(0, 8));
                var expectedDate = receivedDate.Date.AddDays(random.Next(15, 46));
                var status = statuses[random.Next(statuses.Length)];
                var processor = processors[random.Next(processors.Length)];
                var areaName = areaNames[random.Next(areaNames.Count)];
                var caseType = caseTypes[random.Next(caseTypes.Count)];
                var field = fields[random.Next(fields.Count)];
                var recordCode = $"HS-{DateTime.Today:yyyy}-{i:000000}";

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO Records (
    RecordCode, ReceivedDate, ReceiveSource, ReceiverName, SenderName, SenderPhone, ContactAddress,
    AreaName, IncidentAddress, Content, CaseType, ContentGroup, Field, RelatedPerson,
    ExpectedHandlingMethod, SeverityLevel, ExpectedResultDate, PriorityLevel, Status, ProcessorName,
    Note, AdditionalNote, CreatedAt, UpdatedAt)
VALUES (
    $recordCode, $receivedDate, $receiveSource, $receiverName, $senderName, $senderPhone, $contactAddress,
    $areaName, $incidentAddress, $content, $caseType, $contentGroup, $field, $relatedPerson,
    $method, $severity, $expectedDate, $priority, $status, $processor,
    $note, $additionalNote, $createdAt, $updatedAt);
SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$recordCode", recordCode);
                command.Parameters.AddWithValue("$receivedDate", receivedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$receiveSource", receiveSources[random.Next(receiveSources.Count)]);
                command.Parameters.AddWithValue("$receiverName", processors[random.Next(processors.Length)]);
                command.Parameters.AddWithValue("$senderName", senders[random.Next(senders.Length)]);
                command.Parameters.AddWithValue("$senderPhone", $"09{random.Next(10, 99)} {random.Next(100, 999)} {random.Next(100, 999)}");
                command.Parameters.AddWithValue("$contactAddress", $"Ấp {random.Next(1, 9)}, {areaName}, An Giang");
                command.Parameters.AddWithValue("$areaName", areaName);
                command.Parameters.AddWithValue("$incidentAddress", $"Khu vực {random.Next(1, 12)}, {areaName}, An Giang");
                command.Parameters.AddWithValue("$content", BuildRandomContent(caseType, field, areaName));
                command.Parameters.AddWithValue("$caseType", caseType);
                command.Parameters.AddWithValue("$contentGroup", contentGroups[random.Next(contentGroups.Count)]);
                command.Parameters.AddWithValue("$field", field);
                command.Parameters.AddWithValue("$relatedPerson", relatedPersons[random.Next(relatedPersons.Length)]);
                command.Parameters.AddWithValue("$method", methods[random.Next(methods.Count)]);
                command.Parameters.AddWithValue("$severity", priorities[random.Next(priorities.Count)]);
                command.Parameters.AddWithValue("$expectedDate", expectedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$priority", priorities[random.Next(priorities.Count)]);
                command.Parameters.AddWithValue("$status", status);
                command.Parameters.AddWithValue("$processor", processor);
                command.Parameters.AddWithValue("$note", "Hồ sơ mẫu được tạo tự động để phục vụ thiết kế giao diện.");
                command.Parameters.AddWithValue("$additionalNote", "Có thể thay thế bằng dữ liệu thật khi triển khai chức năng nhập liệu.");
                command.Parameters.AddWithValue("$createdAt", receivedDate.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O", CultureInfo.InvariantCulture));
                var recordId = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);

                InsertAttachments(connection, transaction, recordId, random);
                InsertProcessHistory(connection, transaction, recordId, receivedDate, updatedAt, processor, status);
            }

            transaction.Commit();
        }

        private static void InsertAttachments(SqliteConnection connection, SqliteTransaction transaction, int recordId, Random random)
        {
            var files = new[]
            {
                ("Đơn khiếu nại.pdf", "512 KB"),
                ("Hình ảnh hiện trạng.png", "1.2 MB"),
                ("Giấy chứng nhận quyền sử dụng đất.pdf", "842 KB"),
                ("Biên bản làm việc.pdf", "620 KB")
            };

            var fileCount = random.Next(1, 4);
            for (var i = 0; i < fileCount; i++)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO RecordAttachments (RecordId, FileName, FileSize) VALUES ($recordId, $fileName, $fileSize);";
                command.Parameters.AddWithValue("$recordId", recordId);
                command.Parameters.AddWithValue("$fileName", files[i].Item1);
                command.Parameters.AddWithValue("$fileSize", files[i].Item2);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, DateTime receivedDate, DateTime updatedAt, string processor, string status)
        {
            var histories = new List<(string Title, DateTime Time, string Content, bool Done)>
            {
                ("Tiếp nhận", receivedDate, "Tiếp nhận hồ sơ và kiểm tra thông tin ban đầu.", true),
                ("Phân loại", receivedDate.AddHours(2), "Phân loại hồ sơ theo lĩnh vực và loại vụ việc.", true)
            };

            if (status != "Mới tiếp nhận" && status != "Đang phân loại")
            {
                histories.Add(("Phân công", receivedDate.AddHours(5), "Phân công cán bộ phụ trách xử lý hồ sơ.", true));
            }

            if (status == "Đang xác minh" || status == "Chờ kết quả" || status == "Đang chờ bổ sung tài liệu" || status == "Đã giải quyết")
            {
                histories.Add(("Xác minh", updatedAt, "Cập nhật tiến độ xác minh hồ sơ.", status != "Đang xác minh"));
            }

            if (status == "Đã giải quyết")
            {
                histories.Add(("Kết thúc", updatedAt.AddHours(2), "Hoàn tất xử lý và lưu hồ sơ.", true));
            }

            foreach (var history in histories)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO ProcessHistories (RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
VALUES ($recordId, $title, $processedAt, $processor, $content, $isCompleted);";
                command.Parameters.AddWithValue("$recordId", recordId);
                command.Parameters.AddWithValue("$title", history.Title);
                command.Parameters.AddWithValue("$processedAt", history.Time.ToString("O", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$processor", processor);
                command.Parameters.AddWithValue("$content", history.Content);
                command.Parameters.AddWithValue("$isCompleted", history.Done ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private static List<AttachmentDraft> GetAttachments(SqliteConnection connection, int recordId)
        {
            var result = new List<AttachmentDraft>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FileName, FileSize, FilePath FROM RecordAttachments WHERE RecordId = $recordId ORDER BY Id;";
            command.Parameters.AddWithValue("$recordId", recordId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AttachmentDraft
                {
                    FileName = reader.GetString(0),
                    FileSize = reader.GetString(1),
                    FilePath = reader.GetString(2)
                });
            }

            return result;
        }

        private static List<ProcessHistoryItem> GetProcessHistory(SqliteConnection connection, int recordId, string status)
        {
            var result = new List<ProcessHistoryItem>();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Title, ProcessedAt, ProcessorName, Content, IsCompleted
FROM ProcessHistories
WHERE RecordId = $recordId
ORDER BY ProcessedAt;";
            command.Parameters.AddWithValue("$recordId", recordId);
            using var reader = command.ExecuteReader();
            var currentStepTitle = GetProcessStepDefinition(GetProcessStepNumber(status)).Title;
            while (reader.Read())
            {
                var title = reader.GetString(0);
                result.Add(new ProcessHistoryItem
                {
                    Title = title,
                    ProcessedAt = FormatDateTime(reader.GetString(1)),
                    ProcessorName = reader.GetString(2),
                    Content = reader.GetString(3),
                    IsCompleted = reader.GetInt32(4) == 1,
                    IsCurrent = title == currentStepTitle,
                    HasDetails = true
                });
            }

            AddPendingProcessHistoryItems(result, status);
            result.Sort((left, right) => GetHistoryStepOrder(left.Title).CompareTo(GetHistoryStepOrder(right.Title)));
            ApplyHistoryConnectorState(result, status);
            return result;
        }

        private static List<ProcessStep> BuildProcessSteps(string status, IReadOnlyList<ProcessHistoryItem> history)
        {
            var currentStep = GetProcessStepNumber(status);

            return new List<ProcessStep>
            {
                CreateStep(1, currentStep, history),
                CreateStep(2, currentStep, history),
                CreateStep(3, currentStep, history),
                CreateStep(4, currentStep, history),
                CreateStep(5, currentStep, history),
                CreateStep(6, currentStep, history),
                CreateStep(7, currentStep, history)
            };
        }

        private static ProcessStep CreateStep(int stepNumber, int currentStep, IReadOnlyList<ProcessHistoryItem> history)
        {
            var definition = GetProcessStepDefinition(stepNumber);
            var historyItem = history?
                .Where(item => item.Title == definition.Title && !IsPendingProcessHistory(item))
                .LastOrDefault();
            return new ProcessStep
            {
                StepNumber = stepNumber,
                IconGlyph = definition.IconGlyph,
                Title = definition.Title,
                DateText = historyItem?.ProcessedAt?.Split(' ').FirstOrDefault() ?? (stepNumber <= currentStep ? "Đã thực hiện" : "Chưa thực hiện"),
                TimeText = historyItem?.ProcessedAt?.Contains(" ") == true
                    ? historyItem.ProcessedAt.Split(' ').Last()
                    : stepNumber == currentStep ? "Đang thực hiện" : string.Empty,
                IsDone = stepNumber < currentStep,
                IsCurrent = stepNumber == currentStep,
                HasPreviousStep = stepNumber > 1,
                HasNextStep = stepNumber < 7,
                IsPreviousConnectorDone = stepNumber > 1 && stepNumber <= currentStep,
                IsNextConnectorDone = stepNumber < 7 && stepNumber < currentStep
            };
        }

        private static void ApplyHistoryConnectorState(IReadOnlyList<ProcessHistoryItem> history, string status)
        {
            var currentStep = GetProcessStepNumber(status);
            for (var index = 0; index < history.Count; index++)
            {
                var item = history[index];
                var step = GetHistoryStepOrder(item.Title);
                if (step < currentStep)
                {
                    item.IsCompleted = true;
                }

                item.HasNextItem = index < history.Count - 1;
                item.IsNextConnectorDone = step < currentStep;
            }
        }

        private static int GetProcessStepNumber(string status)
        {
            return status switch
            {
                "Mới tiếp nhận" => 1,
                "Đang phân loại" => 2,
                "Đã phân công" => 3,
                "Đang xác minh" => 4,
                "Đang chờ bổ sung tài liệu" => 5,
                "Chờ kết quả" => 6,
                "Đã giải quyết" => 7,
                _ => 4
            };
        }

        private static (string IconGlyph, string Title) GetProcessStepDefinition(int stepNumber)
        {
            return stepNumber switch
            {
                1 => ("\uE8A5", "Tiếp nhận"),
                2 => ("\uE8FD", "Phân loại"),
                3 => ("\uE77B", "Phân công"),
                4 => ("\uE721", "Xác minh"),
                5 => ("\uE916", "Gia hạn"),
                6 => ("\uE73E", "Kết thúc"),
                7 => ("\uE74E", "Lưu hồ sơ"),
                _ => ("\uE8A5", "Tiếp nhận")
            };
        }

        private static int GetHistoryStepOrder(string title)
        {
            for (var step = 1; step <= 7; step++)
            {
                if (GetProcessStepDefinition(step).Title == title)
                {
                    return step;
                }
            }

            return 99;
        }

        private static bool IsPendingProcessHistory(ProcessHistoryItem item)
        {
            return item.ProcessedAt == "Chưa thực hiện";
        }

        private static bool HasProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, string title)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM ProcessHistories WHERE RecordId = $recordId AND Title = $title;";
            command.Parameters.AddWithValue("$recordId", recordId);
            command.Parameters.AddWithValue("$title", title);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static void InsertProcessHistory(SqliteConnection connection, SqliteTransaction transaction, int recordId, string title, DateTime processedAt, string processor, string content, bool isCompleted)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO ProcessHistories (RecordId, Title, ProcessedAt, ProcessorName, Content, IsCompleted)
VALUES ($recordId, $title, $processedAt, $processor, $content, $isCompleted);";
            command.Parameters.AddWithValue("$recordId", recordId);
            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$processedAt", processedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$processor", processor);
            command.Parameters.AddWithValue("$content", content);
            command.Parameters.AddWithValue("$isCompleted", isCompleted ? 1 : 0);
            command.ExecuteNonQuery();
        }

        private static void AddPendingProcessHistoryItems(List<ProcessHistoryItem> result, string status)
        {
            var currentStep = GetProcessStepNumber(status);
            for (var step = 1; step <= 7; step++)
            {
                var definition = GetProcessStepDefinition(step);
                if (result.Any(item => item.Title == definition.Title))
                {
                    continue;
                }

                result.Add(new ProcessHistoryItem
                {
                    Title = definition.Title,
                    ProcessedAt = "Chưa thực hiện",
                    ProcessorName = string.Empty,
                    Content = string.Empty,
                    IsCompleted = false,
                    IsCurrent = step == currentStep,
                    HasDetails = false
                });
            }
        }

        private static void DeleteProcessHistoryFromStep(SqliteConnection connection, SqliteTransaction transaction, int recordId, int firstStep)
        {
            var titles = new List<string>();
            for (var step = firstStep; step <= 7; step++)
            {
                titles.Add(GetProcessStepDefinition(step).Title);
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
DELETE FROM ProcessHistories
WHERE RecordId = $recordId
  AND Title IN ({string.Join(", ", titles.Select((_, index) => "$title" + index))});";
            command.Parameters.AddWithValue("$recordId", recordId);
            for (var index = 0; index < titles.Count; index++)
            {
                command.Parameters.AddWithValue("$title" + index, titles[index]);
            }

            command.ExecuteNonQuery();
        }

        private static string BuildRandomContent(string caseType, string field, string areaName)
        {
            return $"{caseType} liên quan đến lĩnh vực {field.ToLower(CultureInfo.GetCultureInfo("vi-VN"))} tại {areaName}. Nội dung cần được tiếp nhận, phân loại và theo dõi xử lý theo quy trình.";
        }

        private static List<string> ReadAreaDisplayNames(SqliteConnection connection)
        {
            var result = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT AreaType, Name FROM Areas ORDER BY DisplayOrder;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add($"{reader.GetString(0)} {reader.GetString(1)}");
            }

            return result;
        }

        private static List<string> ReadCatalog(SqliteConnection connection, string catalogType)
        {
            var result = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM CatalogItems WHERE CatalogType = $catalogType AND IsActive = 1 ORDER BY DisplayOrder;";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        private static string GetCatalogType(SqliteConnection connection, int id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CatalogType FROM CatalogItems WHERE Id = $id LIMIT 1;";
            command.Parameters.AddWithValue("$id", id);
            return command.ExecuteScalar() as string;
        }

        private static bool CatalogNameExists(SqliteConnection connection, string catalogType, string name, int exceptId = 0)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM CatalogItems
WHERE CatalogType = $catalogType
  AND IsActive = 1
  AND lower(Name) = lower($name)
  AND Id <> $exceptId;";
            command.Parameters.AddWithValue("$catalogType", catalogType);
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$exceptId", exceptId);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        private static int CountRows(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountRecords(SqliteConnection connection, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM Records{BuildDateWhere(fromDate, toDate)};";
            AddDateParameters(command, fromDate, toDate);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string BuildExportWhere(
            SqliteCommand command,
            DateTime? fromDate,
            DateTime? toDate,
            string status,
            string caseType,
            string field,
            string areaName,
            string processorName,
            string searchText)
        {
            var conditions = new List<string>();
            if (fromDate.HasValue)
            {
                conditions.Add("ReceivedDate >= $fromDate");
                command.Parameters.AddWithValue("$fromDate", fromDate.Value.Date.ToString("O", CultureInfo.InvariantCulture));
            }

            if (toDate.HasValue)
            {
                conditions.Add("ReceivedDate <= $toDate");
                command.Parameters.AddWithValue("$toDate", toDate.Value.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
            }

            AddOptionalExportFilter(command, conditions, "Status", "$status", status);
            AddOptionalExportFilter(command, conditions, "CaseType", "$caseType", caseType);
            AddOptionalExportFilter(command, conditions, "Field", "$field", field);
            AddOptionalExportFilter(command, conditions, "AreaName", "$areaName", areaName);
            AddOptionalExportFilter(command, conditions, "ProcessorName", "$processorName", processorName);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(RecordCode LIKE $search OR SenderName LIKE $search OR Content LIKE $search)");
                command.Parameters.AddWithValue("$search", $"%{searchText.Trim()}%");
            }

            return conditions.Count == 0
                ? string.Empty
                : $"WHERE {string.Join(" AND ", conditions)}";
        }

        private static void AddOptionalExportFilter(SqliteCommand command, List<string> conditions, string columnName, string parameterName, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Tất cả")
            {
                return;
            }

            conditions.Add($"{columnName} = {parameterName}");
            command.Parameters.AddWithValue(parameterName, value);
        }

        private static string BuildExportOrderBy(string sortOption)
        {
            return sortOption switch
            {
                "Ngày tiếp nhận cũ nhất trước" => "ReceivedDate ASC, RecordCode ASC",
                "Trạng thái" => "Status ASC, ReceivedDate DESC, RecordCode DESC",
                "Địa bàn" => "AreaName ASC, ReceivedDate DESC, RecordCode DESC",
                _ => "ReceivedDate DESC, RecordCode DESC"
            };
        }

        private static int CountRecordsByStatuses(SqliteConnection connection, DateTime? fromDate, DateTime? toDate, params string[] statuses)
        {
            var parameters = new List<string>();
            using var command = connection.CreateCommand();
            for (var i = 0; i < statuses.Length; i++)
            {
                var parameterName = $"$status{i}";
                parameters.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, statuses[i]);
            }

            command.CommandText = $"SELECT COUNT(*) FROM Records WHERE Status IN ({string.Join(",", parameters)}){BuildDateCondition(fromDate, toDate)};";
            AddDateParameters(command, fromDate, toDate);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static int CountOverdueOpenRecords(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM Records
WHERE Status <> 'Đã giải quyết'
  AND ExpectedResultDate <> ''
  AND ExpectedResultDate < $today;";
            command.Parameters.AddWithValue("$today", DateTime.Today.ToString("O", CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string BuildDateWhere(DateTime? fromDate, DateTime? toDate)
        {
            var condition = BuildDateCondition(fromDate, toDate);
            return string.IsNullOrWhiteSpace(condition)
                ? string.Empty
                : $" WHERE{condition.Substring(4)}";
        }

        private static string BuildDateCondition(DateTime? fromDate, DateTime? toDate)
        {
            var conditions = new List<string>();
            if (fromDate.HasValue)
            {
                conditions.Add("ReceivedDate >= $fromDate");
            }

            if (toDate.HasValue)
            {
                conditions.Add("ReceivedDate <= $toDate");
            }

            return conditions.Count == 0
                ? string.Empty
                : $" AND {string.Join(" AND ", conditions)}";
        }

        private static void AddDateParameters(SqliteCommand command, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                command.Parameters.AddWithValue("$fromDate", fromDate.Value.Date.ToString("O", CultureInfo.InvariantCulture));
            }

            if (toDate.HasValue)
            {
                command.Parameters.AddWithValue("$toDate", toDate.Value.Date.AddDays(1).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
            }
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static void TryAddColumn(SqliteConnection connection, string tableName, string columnName, string definition)
        {
            try
            {
                ExecuteNonQuery(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};");
            }
            catch (SqliteException ex)
            {
                AppLogger.Warning("Database", "TryAddColumn", $"Could not add column {tableName}.{columnName}. It may already exist.", ex);
                // Existing local databases already have the column after the first migration run.
            }
        }

        private static string FormatDate(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
                : value;
        }

        private static string FormatDateTime(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? date.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"))
                : value;
        }

        private static DateTime ParseDisplayDate(string value)
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var exactDate))
            {
                return exactDate;
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                ? date
                : DateTime.Today;
        }

        private static string GetStatusColor(string status)
        {
            return status switch
            {
                "Đã giải quyết" => "#24A148",
                "Đang xác minh" => "#2F73FF",
                "Đang phân loại" => "#F5B132",
                "Đã phân công" => "#0B5CFF",
                "Chờ kết quả" => "#7B4DE3",
                "Đang chờ bổ sung tài liệu" => "#FF5A1F",
                "Chuyển cơ quan khác" => "#1F4AB8",
                _ => "#5C6B91"
            };
        }
    }
}
