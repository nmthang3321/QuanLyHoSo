using System;
using QuanLyHoSo.Infrastructure.Updates;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class ApplicationUpdaterTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void BuildUpdaterScript_ShouldCopyPackageRestartExecutableAndLogFailures()
        {
            var script = ApplicationUpdater.BuildUpdaterScript(
                123,
                @"C:\Updates\QuanLyHoSo-Server-1.2.3.zip",
                @"C:\Program Files\QuanLyHoSo Server\",
                @"C:\Program Files\QuanLyHoSo Server\QuanLyHoSo.Server.exe",
                new[] { "--url", "http://0.0.0.0:5055" },
                @"C:\Logs",
                @"C:\Temp\update.ps1");

            Assert.Contains("Wait-Process -Id $processId", script, StringComparison.Ordinal);
            Assert.Contains("Expand-Archive", script, StringComparison.Ordinal);
            Assert.Contains("Copy-Item", script, StringComparison.Ordinal);
            Assert.Contains("Start-Process -FilePath $exePath", script, StringComparison.Ordinal);
            Assert.Contains("http://0.0.0.0:5055", script, StringComparison.Ordinal);
            Assert.Contains("Set-Content -LiteralPath $logPath", script, StringComparison.Ordinal);
        }
    }
}
