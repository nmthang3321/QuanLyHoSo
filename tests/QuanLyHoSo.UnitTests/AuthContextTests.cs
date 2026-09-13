using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class AuthContextTests : System.IDisposable
    {
        public AuthContextTests() => AuthContext.SignOut();
        public void Dispose() => AuthContext.SignOut();

        [Theory]
        [InlineData(UserRoles.Admin, true, true, true, false)]
        [InlineData(UserRoles.Officer, true, false, false, true)]
        [InlineData(UserRoles.Leader, false, false, false, false)]
        [InlineData("Unknown", false, false, false, false)]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void RolePermissions_ShouldMatchExistingAuthorizationMatrix(
            string role, bool canWrite, bool canCreate, bool canDelete, bool isOfficer)
        {
            AuthContext.SignIn(User(role, "Nguyễn Văn A"));

            Assert.Equal(canWrite, AuthContext.CanWrite);
            Assert.Equal(canCreate, AuthContext.CanCreateRecord);
            Assert.Equal(canDelete, AuthContext.CanDeleteRecord);
            Assert.Equal(isOfficer, AuthContext.IsOfficer);
        }

        [Theory]
        [InlineData("Nguyễn Văn A", true)]
        [InlineData("  nguyễn văn a  ", true)]
        [InlineData("Nguyễn Văn B", false)]
        [InlineData(null, false)]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void CanAccessRecord_ForOfficer_ShouldOnlyAllowAssignedDisplayName(string processor, bool expected)
        {
            AuthContext.SignIn(User(UserRoles.Officer, "Nguyễn Văn A"));

            Assert.Equal(expected, AuthContext.CanAccessRecord(processor));
            Assert.Equal(expected, AuthContext.CanEditRecord(processor));
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void BeginRequestScope_WhenDisposed_ShouldRestoreSignedInUser()
        {
            var signedIn = User(UserRoles.Admin, "Admin");
            AuthContext.SignIn(signedIn);

            using (AuthContext.BeginRequestScope(User(UserRoles.Officer, "Cán bộ")))
            {
                Assert.True(AuthContext.IsOfficer);
                Assert.Equal("Cán bộ", AuthContext.CurrentDisplayName);
            }

            Assert.Same(signedIn, AuthContext.CurrentUser);
            Assert.True(AuthContext.IsAdmin);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void SignOut_ShouldClearIdentityAndPermissions()
        {
            AuthContext.SignIn(User(UserRoles.Admin, "Admin"));

            AuthContext.SignOut();

            Assert.False(AuthContext.IsAuthenticated);
            Assert.False(AuthContext.CanWrite);
            Assert.Equal(string.Empty, AuthContext.CurrentDisplayName);
        }

        private static AppUser User(string role, string displayName) => new AppUser
        {
            UserName = "test",
            DisplayName = displayName,
            Role = role,
            IsActive = true
        };
    }
}
