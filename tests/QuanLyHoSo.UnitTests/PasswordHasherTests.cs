using QuanLyHoSo.Infrastructure.Security;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class PasswordHasherTests
    {
        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void HashPassword_WhenPasswordIsUnicode_ShouldVerifyExactValue()
        {
            const string password = "Mật-khẩu-安全-123!";

            var hash = PasswordHasher.HashPassword(password);

            Assert.True(PasswordHasher.VerifyPassword(password, hash));
            Assert.False(PasswordHasher.VerifyPassword(password + "x", hash));
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void HashPassword_WhenCalledTwice_ShouldUseDifferentSalts()
        {
            var first = PasswordHasher.HashPassword("same-password");
            var second = PasswordHasher.HashPassword("same-password");

            Assert.NotEqual(first, second);
            Assert.True(PasswordHasher.VerifyPassword("same-password", first));
            Assert.True(PasswordHasher.VerifyPassword("same-password", second));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-hash")]
        [InlineData("100000.invalid.invalid")]
        [InlineData("0.YQ==.YQ==")]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void VerifyPassword_WhenHashIsMalformed_ShouldReturnFalse(string storedHash)
        {
            Assert.False(PasswordHasher.VerifyPassword("password", storedHash));
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void HashPassword_WhenPasswordIsNull_ShouldPreserveExistingNullAsEmptyBehavior()
        {
            var hash = PasswordHasher.HashPassword(null);

            Assert.True(PasswordHasher.VerifyPassword(string.Empty, hash));
            Assert.True(PasswordHasher.VerifyPassword(null, hash));
        }
    }
}
