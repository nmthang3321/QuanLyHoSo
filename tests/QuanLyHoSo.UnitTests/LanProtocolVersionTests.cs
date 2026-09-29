using System;
using QuanLyHoSo.Infrastructure.Network;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class LanProtocolVersionTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        public void Current_ShouldExposeAThreePartProductVersion()
        {
            Assert.True(Version.TryParse(LanProtocolVersion.Current, out var version));

            Assert.True(version.Major >= 0);
            Assert.True(version.Minor >= 0);
            Assert.True(version.Build >= 0);
        }
    }
}
