using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "Regression")]
    public sealed class RecordCodeRulesTests
    {
        [Fact]
        public void Normalize_ShouldTrimAndPreserveUserInput()
        {
            Assert.Equal("hs 2026/Đơn-abc", RecordCodeRules.Normalize("  hs 2026/Đơn-abc  "));
        }
    }
}
