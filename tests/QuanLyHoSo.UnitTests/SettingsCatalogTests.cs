using System.Collections.Generic;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class SettingsCatalogTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        public void CatalogGroups_ShouldNotExposeProtectedPriorityCatalog()
        {
            var service = new Mock<IApplicationDataService>();
            service.SetupGet(item => item.DatabasePath).Returns("test.db");
            service.Setup(item => item.CountCatalogItemsByType()).Returns(new Dictionary<string, int>());

            var viewModel = new SettingsViewModel(service.Object);

            Assert.Equal(6, viewModel.CatalogGroups.Count);
            Assert.DoesNotContain(viewModel.CatalogGroups, item => item.CatalogType == "Priority");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UpdateScope_ShouldExposeServerChoiceOnlyToAdminInClientMode()
        {
            var service = CreateService();
            try
            {
                AuthContext.SignIn(new AppUser { Role = UserRoles.Officer });
                var officerViewModel = new SettingsViewModel(service.Object);
                Assert.False(officerViewModel.CanChooseServerUpdate);
                Assert.False(officerViewModel.IsServerAndClientUpdateSelected);

                AuthContext.SignIn(new AppUser { Role = UserRoles.Admin });
                var adminViewModel = new SettingsViewModel(service.Object);
                Assert.Equal(AppPathSettings.Current.IsClientMode, adminViewModel.CanChooseServerUpdate);
                Assert.Equal(2, adminViewModel.UpdateScopes.Count);
                Assert.False(adminViewModel.IsServerAndClientUpdateSelected);
            }
            finally
            {
                AuthContext.SignOut();
            }
        }

        private static Mock<IApplicationDataService> CreateService()
        {
            var service = new Mock<IApplicationDataService>();
            service.SetupGet(item => item.DatabasePath).Returns("test.db");
            service.Setup(item => item.CountCatalogItemsByType()).Returns(new Dictionary<string, int>());
            return service;
        }
    }
}
