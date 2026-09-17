using QuanLyHoSo.Infrastructure.Network;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class LanProtocolVersionTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        public void IsCompatible_ShouldRequireExactThreePartVersion()
        {
            Assert.True(LanProtocolVersion.IsCompatible(LanProtocolVersion.Current));
            Assert.True(LanProtocolVersion.IsCompatible(LanProtocolVersion.Current + ".0"));
            Assert.False(LanProtocolVersion.IsCompatible("0.0.0"));
            Assert.False(LanProtocolVersion.IsCompatible(null));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void BuildMismatchMessage_ShouldIdentifyBothVersions()
        {
            var message = LanProtocolVersion.BuildMismatchMessage("0.9.0");

            Assert.Contains("0.9.0", message);
            Assert.Contains(LanProtocolVersion.Current, message);
        }
    }
}
