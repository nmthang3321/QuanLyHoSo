using System;
using System.Linq;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class ProcessingQueueDateFilterTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        public void ProcessingQueue_ShouldApplyReceivedDateRangeToCardsCountAndList()
        {
            using var db = new TestDatabase();
            var oldRecord = db.NewRecord("processing-date-old");
            oldRecord.ReceivedDate = "31/12/2025";
            var oldCode = db.Service.SaveRecordForm(oldRecord);

            var currentRecord = db.NewRecord("processing-date-current");
            currentRecord.ReceivedDate = "01/01/2026";
            var currentCode = db.Service.SaveRecordForm(currentRecord);

            var fromDate = new DateTime(2026, 1, 1);
            var toDate = new DateTime(2026, 12, 31);
            var records = db.Service.GetProcessingQueueRecords(
                null, null, null, null, null, 0, 20, fromDate, toDate);

            Assert.Equal(1, db.Service.CountProcessingQueueRecords(
                null, null, null, null, null, fromDate, toDate));
            Assert.Equal("1", db.Service.GetProcessingQueueMetrics(fromDate, toDate)
                .Single(metric => metric.FilterKey == "All").Value);
            Assert.Contains(records, record => record.RecordCode == currentCode);
            Assert.DoesNotContain(records, record => record.RecordCode == oldCode);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void ProcessedCard_ShouldCountAndListResolvedRecordsOnly()
        {
            using var db = new TestDatabase();
            var resolvedCode = db.Service.SaveRecordForm(db.NewRecord("processing-resolved"));
            var openCode = db.Service.SaveRecordForm(db.NewRecord("processing-open"));
            db.Service.UpdateProcessingRecord(
                resolvedCode,
                "Đã giải quyết",
                new DateTime(2026, 9, 20),
                "Integration Admin",
                "Đã hoàn tất xử lý.",
                string.Empty,
                string.Empty,
                Array.Empty<AttachmentDraft>());

            var metric = db.Service.GetProcessingQueueMetrics()
                .Single(item => item.FilterKey == "Processed");
            var records = db.Service.GetProcessingQueueRecords(
                cardFilterKey: "Processed",
                take: 20);

            Assert.Equal("1", metric.Value);
            Assert.Equal(1, db.Service.CountProcessingQueueRecords(cardFilterKey: "Processed"));
            Assert.Contains(records, record => record.RecordCode == resolvedCode);
            Assert.DoesNotContain(records, record => record.RecordCode == openCode);
        }
    }
}
