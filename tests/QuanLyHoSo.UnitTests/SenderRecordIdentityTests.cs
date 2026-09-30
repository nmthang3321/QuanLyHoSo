using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class SenderRecordIdentityTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void UnconfirmedSender_ShouldNeverBeLinkableEvenForSameCase()
        {
            var row = new SenderRecordHistory { RecordCode = "HS-2026-000001", IsSameCase = true, IsConfirmedSender = false };

            Assert.False(row.CanLinkAsResubmission);
            Assert.Contains("chưa xác thực", row.SenderMatchDisplay);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ConfirmedSameCaseRow_ShouldRemainLinkable()
        {
            var row = new SenderRecordHistory { RecordCode = "HS-2026-000001", IsSameCase = true, IsConfirmedSender = true };

            Assert.True(row.CanLinkAsResubmission);
            Assert.Equal("Đã xác thực", row.SenderMatchDisplay);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void DefaultRows_ShouldStayConfirmedForBackwardCompatibility()
        {
            // Payloads from an older server carry no IsConfirmedSender field; the
            // deserialized default must keep the previous linking behavior.
            var row = new SenderRecordHistory { IsSameCase = true };

            Assert.True(row.IsConfirmedSender);
            Assert.True(row.CanLinkAsResubmission);
        }
    }
}
