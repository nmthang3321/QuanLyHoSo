using System;
using System.Linq;
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
    }
}
