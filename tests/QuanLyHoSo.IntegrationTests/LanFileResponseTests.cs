using System;
using QuanLyHoSo.Infrastructure.Network;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class LanFileResponseTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Attachments")]
        public void ContentDisposition_WithVietnameseFileName_ShouldUseAsciiHeaderAndUtf8FileName()
        {
            const string fileName = "807_Thái Quốc Quân_Lưu đơn, trả lời X05_0001.pdf";

            var header = LanDataServer.BuildContentDispositionHeader(fileName);

            Assert.All(header, character => Assert.InRange((int)character, 0, 127));
            Assert.Contains("filename*=UTF-8''", header, StringComparison.Ordinal);
            Assert.Contains(Uri.EscapeDataString(fileName), header, StringComparison.Ordinal);
            Assert.EndsWith(".pdf", Uri.UnescapeDataString(header.Split("UTF-8''")[1]), StringComparison.OrdinalIgnoreCase);
        }
    }
}
