using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "Regression")]
    public sealed class RecordCodeRulesTests
    {
        [Theory]
        [InlineData("HS-2026-000001")]
        [InlineData("hs-1999-999999")]
        [InlineData(" HS-2030-123456 ")]
        public void IsValid_ShouldAcceptExpectedFormat(string value)
        {
            Assert.True(RecordCodeRules.IsValid(value));
        }

        [Theory]
        [InlineData("HS-26-000001")]
        [InlineData("HS-2026-00001")]
        [InlineData("HS-2026-0000001")]
        [InlineData("CV-2026-000001")]
        [InlineData("HS-202A-000001")]
        [InlineData("")]
        public void IsValid_ShouldRejectUnexpectedFormat(string value)
        {
            Assert.False(RecordCodeRules.IsValid(value));
        }

        [Fact]
        public void Normalize_ShouldTrimAndUppercaseInput()
        {
            Assert.Equal("HS-2026-000123", RecordCodeRules.Normalize("  hs-2026-000123  "));
        }
    }
}
