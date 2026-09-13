using System;
using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using Xunit;

namespace QuanLyHoSo.UITests
{
    public sealed class LoginStartupTests
    {
        [Fact]
        [Trait("Category", "UI")]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Critical")]
        public void ApplicationStartup_ShouldExposeStableLoginControlsAndShutDownCleanly()
        {
            var executable = Environment.GetEnvironmentVariable("QUANLYHOSO_UI_EXE");
            Assert.True(File.Exists(executable),
                "Set QUANLYHOSO_UI_EXE by running tests/Scripts/run-ui.ps1.");

            var isolatedRoot = Path.Combine(Path.GetTempPath(), "QuanLyHoSo.UITests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(isolatedRoot, "Settings"));
            File.WriteAllText(Path.Combine(isolatedRoot, "Settings", "path-settings.json"),
                "{\"DataAccessMode\":\"Client\",\"AdminServerUrl\":\"http://127.0.0.1:1\"}");

            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable)
            };
            startInfo.EnvironmentVariables["QUANLYHOSO_TEST_ROOT"] = isolatedRoot;

            Application application = null;
            try
            {
                application = Application.Launch(startInfo);
                using var automation = new UIA3Automation();
                var window = application.GetMainWindow(automation, TimeSpan.FromSeconds(15));

                Assert.NotNull(window);
                Assert.Equal("Phần mềm quản lý hồ sơ", window.Title);
                Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId("Login.UserName")));
                Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId("Login.Password")));
                Assert.NotNull(window.FindFirstDescendant(cf => cf.ByAutomationId("Login.SignIn")));

                window.Close();
                Assert.True(application.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(10)) || application.HasExited);
            }
            finally
            {
                if (application != null && !application.HasExited)
                {
                    application.Close();
                }

                if (Directory.Exists(isolatedRoot))
                {
                    Directory.Delete(isolatedRoot, true);
                }
            }
        }
    }
}
