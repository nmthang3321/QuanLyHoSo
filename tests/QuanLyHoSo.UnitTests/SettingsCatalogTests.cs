using System.Collections.Generic;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
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
    }
}
