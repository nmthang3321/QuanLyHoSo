using System;
using System.Collections.Generic;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class DefaultDateFilterTests : IDisposable
    {
        public DefaultDateFilterTests() => AuthContext.SignIn(new AppUser
        {
            Id = 1,
            UserName = "admin",
            DisplayName = "Admin",
            Role = UserRoles.Admin,
            IsActive = true
        });

        public void Dispose() => AuthContext.SignOut();

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void MainDataPages_ShouldUseConfiguredDefaultDateRanges()
        {
            var service = CreateService();
            var expectedFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var expectedTo = expectedFrom.AddMonths(1).AddDays(-1);

            using var dashboard = new DashboardViewModel(service.Object);
            using var records = new RecordListViewModel(service.Object, () => { }, _ => { }, (_, _) => { });
            using var processing = new RecordProcessingViewModel(service.Object);
            using var staff = new StaffTrackingViewModel(service.Object);

            Assert.Equal("Năm này", dashboard.SelectedDateFilter);
            Assert.Equal(new DateTime(DateTime.Today.Year, 1, 1), dashboard.FromDate);
            Assert.Equal(new DateTime(DateTime.Today.Year, 12, 31), dashboard.ToDate);
            Assert.Equal(expectedFrom, records.FromDate);
            Assert.Equal(expectedTo, records.ToDate);
            Assert.Equal("Năm này", processing.SelectedDateFilter);
            Assert.Equal(new DateTime(DateTime.Today.Year, 1, 1), processing.FromDate);
            Assert.Equal(new DateTime(DateTime.Today.Year, 12, 31), processing.ToDate);
            Assert.Equal("Tháng này", staff.SelectedDateFilter);
            Assert.Equal(expectedFrom, staff.FromDate);
            Assert.Equal(expectedTo, staff.ToDate);
        }

        private static Mock<IApplicationDataService> CreateService()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetDashboardMetrics(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<DashboardMetric>());
            service.Setup(x => x.GetStatusStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<StatusStat>());
            service.Setup(x => x.GetTopAreas(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<AreaStat>());
            service.Setup(x => x.GetReceivedTrendStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<TrendStat>());
            service.Setup(x => x.GetRecentRecords(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>())).Returns(Array.Empty<RecentRecord>());
            service.Setup(x => x.GetProcessingQueueMetrics(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<DashboardMetric>());
            service.Setup(x => x.GetProcessingQueueRecords(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<ProcessingQueueRecord>());
            service.Setup(x => x.CountProcessingQueueRecords(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(0);
            service.Setup(x => x.GetFilteredRecords(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Array.Empty<RecentRecord>());
            service.Setup(x => x.GetStaffPerformanceRows(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<StaffPerformanceRow>());
            service.Setup(x => x.GetStaffDeadlineStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<StatusStat>());
            service.Setup(x => x.GetTopOfficers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(Array.Empty<StaffPerformanceBarRow>());
            service.Setup(x => x.GetLeadershipNotices(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(new StaffNotificationPage
            {
                Items = Array.Empty<StaffNotification>()
            });
            return service;
        }
    }
}
