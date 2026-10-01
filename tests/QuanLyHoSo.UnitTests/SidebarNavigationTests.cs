using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class SidebarNavigationTests : IDisposable
    {
        public SidebarNavigationTests() => AuthContext.SignOut();

        public void Dispose() => AuthContext.SignOut();

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void SidebarNavigation_ShouldReusePreviouslyCreatedPage()
        {
            var service = CreateService();
            using var shell = new ShellViewModel(service.Object);
            CompleteSignIn(shell);

            var settings = shell.SettingsNavigationItem;
            settings.Command.Execute(null);
            var firstSettingsViewModel = shell.CurrentViewModel;

            shell.NavigationItems.Single(item => item.Key == "Dashboard").Command.Execute(null);
            settings.Command.Execute(null);

            Assert.Same(firstSettingsViewModel, shell.CurrentViewModel);
            service.Verify(x => x.CountCatalogItemsByType(), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void RecordListSidebarNavigation_ShouldPreserveFiltersAndColumnsButCloseDetail()
        {
            var service = CreateService();
            using var shell = new ShellViewModel(service.Object);
            CompleteSignIn(shell);
            var recordListItem = shell.NavigationItems.Single(item => item.Key == "RecordList");
            recordListItem.Command.Execute(null);
            var records = Assert.IsType<RecordListViewModel>(shell.CurrentViewModel);
            records.FromDate = new DateTime(2020, 1, 1);
            records.ToDate = new DateTime(2020, 12, 31);
            records.SelectedStatus = "Đã giải quyết";
            records.SearchText = "không giữ lại";
            records.IsFilterPanelOpen = true;
            records.IsAreaColumnVisible = false;
            records.IsCommanderApproverColumnVisible = true;
            typeof(RecordListViewModel)
                .GetMethod("ViewRecord", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(records, new object[] { "HS-2026-000001" });

            shell.NavigationItems.Single(item => item.Key == "Dashboard").Command.Execute(null);
            recordListItem.Command.Execute(null);

            Assert.Equal(new DateTime(2020, 1, 1), records.FromDate);
            Assert.Equal(new DateTime(2020, 12, 31), records.ToDate);
            Assert.Equal("Đã giải quyết", records.SelectedStatus);
            Assert.Equal("không giữ lại", records.SearchText);
            Assert.False(records.IsAreaColumnVisible);
            Assert.True(records.IsCommanderApproverColumnVisible);
            Assert.False(records.IsFilterPanelOpen);
            Assert.Null(records.SelectedRecordDetail);
        }

        private static Mock<IApplicationDataService> CreateService()
        {
            var service = new Mock<IApplicationDataService>();
            service.SetupGet(x => x.DatabasePath).Returns(string.Empty);
            service.Setup(x => x.CountCatalogItemsByType()).Returns(new Dictionary<string, int>());
            service.Setup(x => x.GetDashboardMetrics(
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(Array.Empty<DashboardMetric>());
            service.Setup(x => x.GetStatusStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(Array.Empty<StatusStat>());
            service.Setup(x => x.GetTopAreas(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(Array.Empty<AreaStat>());
            service.Setup(x => x.GetReceivedTrendStats(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .Returns(Array.Empty<TrendStat>());
            service.Setup(x => x.GetRecentRecords(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .Returns(Array.Empty<RecentRecord>());
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new[] { "Tất cả" });
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>()))
                .Returns(new[] { "Tất cả" });
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>()))
                .Returns(new[] { "Tất cả" });
            service.Setup(x => x.GetFilteredRecords(
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Array.Empty<RecentRecord>());
            service.Setup(x => x.GetRecordForm(It.IsAny<string>())).Returns(new RecordFormDraft { RecordCode = "HS-2026-000001" });
            return service;
        }

        private static void CompleteSignIn(ShellViewModel shell)
        {
            var method = typeof(ShellViewModel).GetMethod("CompleteSignIn", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(shell, new object[]
            {
                new AppUser
                {
                    Id = 1,
                    UserName = "admin",
                    DisplayName = "Admin",
                    Role = UserRoles.Admin,
                    IsActive = true
                }
            });
        }
    }
}
