using System;
using System.IO;
using System.Linq;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class DatabaseInitializationAndAuthenticationTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Database")]
        public void Initialize_OnFreshPath_ShouldCreateUsableDatabaseAndSeedRequiredData()
        {
            using var database = new TestDatabase();

            Assert.True(File.Exists(database.DatabasePath));
            Assert.NotEmpty(database.Service.GetAreaNames());
            Assert.NotEmpty(database.Service.GetCatalogValues("CaseType"));
            Assert.True(database.Service.CountRecords() > 0);
            Assert.NotNull(database.Service.AuthenticateUser("admin", "admin123"));
        }

        [Theory]
        [InlineData("admin", "wrong-password")]
        [InlineData("missing-user", "admin123")]
        [InlineData("", "admin123")]
        [InlineData(null, null)]
        [Trait("Category", "Security")]
        [Trait("Category", "Integration")]
        public void AuthenticateUser_WhenCredentialsAreInvalid_ShouldReturnNull(string userName, string password)
        {
            using var database = new TestDatabase();

            Assert.Null(database.Service.AuthenticateUser(userName, password));
        }

        [Fact]
        [Trait("Category", "Security")]
        [Trait("Category", "Integration")]
        public void SaveUser_WhenUnicodeUserIsCreated_ShouldAuthenticateAndDeactivateCleanly()
        {
            using var database = new TestDatabase();
            var user = new AppUser
            {
                UserName = "canbo.test",
                DisplayName = "Trần Thị Ánh",
                Role = UserRoles.Officer,
                IsActive = true
            };

            Assert.True(database.Service.SaveUser(user, "Strong-123"));
            var persisted = database.Service.GetUsers().Single(item => item.UserName == "canbo.test");
            var authenticated = database.Service.AuthenticateUser("CANBO.TEST", "Strong-123");
            Assert.Equal("Trần Thị Ánh", authenticated.DisplayName);
            Assert.True(authenticated.MustChangePassword);

            Assert.True(database.Service.DeleteUser(persisted.Id));
            Assert.Null(database.Service.AuthenticateUser("canbo.test", "Strong-123"));
        }

        [Fact]
        [Trait("Category", "Security")]
        [Trait("Category", "Regression")]
        public void SaveUser_WhenUsernameDiffersOnlyByCase_ShouldRejectDuplicate()
        {
            using var database = new TestDatabase();
            database.Service.SaveUser(new AppUser
            {
                UserName = "duplicate.user",
                DisplayName = "Người dùng thứ nhất",
                Role = UserRoles.Officer,
                IsActive = true
            }, "Password-1");

            Assert.Throws<InvalidOperationException>(() => database.Service.SaveUser(new AppUser
            {
                UserName = "DUPLICATE.USER",
                DisplayName = "Người dùng thứ hai",
                Role = UserRoles.Officer,
                IsActive = true
            }, "Password-2"));
            Assert.Single(database.Service.GetUsers(), item =>
                string.Equals(item.UserName, "duplicate.user", StringComparison.OrdinalIgnoreCase));
        }
    }
}
