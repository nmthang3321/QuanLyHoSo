using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class DashboardLoadingTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public async Task Reload_ShouldExposeLoadingStateAndStartIndependentQueriesConcurrently()
        {
            using var started = new CountdownEvent(6);
            using var release = new ManualResetEventSlim(false);
            var service = new Mock<IApplicationDataService>();

            service.Setup(x => x.GetDashboardMetrics(
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(() => WaitAndReturn(started, release, Array.Empty<DashboardMetric>()));
            service.Setup(x => x.GetStatusStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(() => WaitAndReturn(started, release, Array.Empty<StatusStat>()));
            service.Setup(x => x.GetTopAreas(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(() => WaitAndReturn(started, release, Array.Empty<AreaStat>()));
            service.Setup(x => x.GetReceivedTrendStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(() => WaitAndReturn(started, release, Array.Empty<TrendStat>()));
            service.Setup(x => x.CountRecords(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(() => WaitAndReturn(started, release, 0));
            service.Setup(x => x.GetRecentRecords(
                    It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .Returns(() => WaitAndReturn(started, release, Array.Empty<RecentRecord>()));

            using var viewModel = new DashboardViewModel(service.Object);

            Assert.True(viewModel.IsLoading);
            var allQueriesStarted = started.Wait(TimeSpan.FromSeconds(3));
            release.Set();

            Assert.True(allQueriesStarted);
            await WaitUntilAsync(() => !viewModel.IsLoading, TimeSpan.FromSeconds(3));
            Assert.Equal("0", viewModel.TotalRecordsText);
        }

        private static T WaitAndReturn<T>(CountdownEvent started, ManualResetEventSlim release, T value)
        {
            started.Signal();
            release.Wait(TimeSpan.FromSeconds(5));
            return value;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            Assert.True(condition());
        }
    }
}
