using System.Linq;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    [Trait("Category", "Unit")]
    public sealed class StaffTrackingPerformanceBarsTests
    {
        private static StaffPerformanceRow Row(string name, string onTime, int kpi) => new()
        {
            Name = name,
            OnTimeRateText = onTime,
            KpiPercent = kpi,
            AssignedCount = 10
        };

        [Fact]
        public void EvaluateTopOfficers_ShouldCombineQualityAndQuantity()
        {
            var rows = new[]
            {
                Row("A", "96%", 0), Row("B", "98%", 0), Row("C", "75%", 0)
            };
            rows[0].CompletedCount = 10;
            rows[1].CompletedCount = 2;
            rows[2].CompletedCount = 10;

            var top = AppDataService.EvaluateTopOfficers(rows, 5);

            // A: 0.6*96 + 0.4*100 = 97.6 -> 98 (nhiều việc + chuẩn)
            // C: 0.6*75 + 0.4*100 = 85
            // B: 0.6*98 + 0.4*20  = 66.8 -> 67 (ít việc dù đúng hạn cao nhất)
            Assert.Equal(new[] { "A", "C", "B" }, top.Select(x => x.StaffName));
            Assert.Equal(new[] { 98, 85, 67 }, top.Select(x => x.CompositeScore));
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldExcludeOfficersWithoutAnyRecord()
        {
            var rows = new[]
            {
                Row("A", "96%", 0),
                Row("TRỐNG", "100%", 0)
            };
            rows[0].CompletedCount = 10;
            rows[1].AssignedCount = 0;
            rows[1].CompletedCount = 0;
            rows[1].DeadlineTrackedCount = 0;

            var top = AppDataService.EvaluateTopOfficers(rows, 5);

            Assert.Single(top);
            Assert.Equal("A", top[0].StaffName);
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldTakeRequestedCount()
        {
            var rows = Enumerable.Range(1, 8)
                .Select(i =>
                {
                    var row = Row($"CB{i:00}", $"{100 - i * 5}%", 0);
                    row.CompletedCount = 100 - i;
                    return row;
                })
                .ToArray();

            var top = AppDataService.EvaluateTopOfficers(rows, 5);

            Assert.Equal(5, top.Count);
            Assert.Equal(new[] { "CB01", "CB02", "CB03", "CB04", "CB05" }, top.Select(x => x.StaffName));
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldTieBreakByOnTimeThenCompletedThenName()
        {
            var rows = new[]
            {
                Row("B", "90%", 0), Row("A", "90%", 0), Row("C", "90%", 0), Row("D", "90%", 0)
            };
            foreach (var row in rows) row.CompletedCount = 5;
            rows[1].CompletedCount = 7;
            rows[2].CompletedCount = 7;

            var top = AppDataService.EvaluateTopOfficers(rows, 5);

            Assert.Equal(new[] { "A", "C", "B", "D" }, top.Select(x => x.StaffName));
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldCarrySampleNumbersForTooltip()
        {
            var rows = new[] { Row("A", "96%", 0) };
            rows[0].CompletedCount = 18;
            rows[0].OnTimeCompletedCount = 48;
            rows[0].DeadlineTrackedCount = 50;

            var top = AppDataService.EvaluateTopOfficers(rows, 5);

            Assert.Equal(48, top[0].OnTimeCompletedCount);
            Assert.Equal(50, top[0].DeadlineTrackedCount);
            Assert.Equal(18, top[0].CompletedCount);
            Assert.Equal(100, top[0].QuantityPercent);
            Assert.Equal(265, top[0].BarWidth); // điểm 98/100 -> 98% thanh tối đa
            Assert.Equal("Tốt", top[0].StatusText);
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldHideOtherOfficersDetailsFromOfficerView()
        {
            var rows = new[] { Row("A", "96%", 0), Row("B", "90%", 0) };
            rows[0].CompletedCount = 10;
            rows[1].CompletedCount = 8;

            var officerView = AppDataService.EvaluateTopOfficers(rows, 5, showAllDetails: false, viewerName: "A");

            Assert.True(officerView.ElementAt(0).ShowDetail);  // chính mình -> xem được chi tiết
            Assert.False(officerView.ElementAt(1).ShowDetail); // người khác -> chỉ thấy điểm

            var leaderView = AppDataService.EvaluateTopOfficers(rows, 5);
            Assert.All(leaderView, x => Assert.True(x.ShowDetail));
        }

        [Fact]
        public void EvaluateTopOfficers_ShouldHandleInvalidTextAndEmptyInput()
        {
            var broken = new[] { Row("A", "không rõ", 0) };
            broken[0].CompletedCount = 3;

            var top = AppDataService.EvaluateTopOfficers(broken, 5);

            // onTime 0% nhưng hoàn thành 3/3 (khối lượng 100%) -> 0.6*0 + 0.4*100 = 40
            Assert.Equal(0, top[0].OnTimePercent);
            Assert.Equal(100, top[0].QuantityPercent);
            Assert.Equal(40, top[0].CompositeScore);
            Assert.Equal(108, top[0].BarWidth);

            Assert.Empty(AppDataService.EvaluateTopOfficers(null, 5));
            Assert.Empty(AppDataService.EvaluateTopOfficers(System.Array.Empty<StaffPerformanceRow>(), 5));
        }
    }
}
