using System;
using System.IO;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.IntegrationTests
{
    internal sealed class TestDatabase : IDisposable
    {
        public TestDatabase(bool seedSampleRecords = false)
        {
            RootPath = Path.Combine(Path.GetTempPath(), "QuanLyHoSo.Tests", Guid.NewGuid().ToString("N"));
            DatabasePath = Path.Combine(RootPath, "data", "test.db");
            Directory.CreateDirectory(RootPath);
            try
            {
                AppPathSettings.UseServerMode(DatabasePath, Path.Combine(RootPath, "logs"), "http://127.0.0.1:0");
                Service = new AppDataService(DatabasePath);
                Service.Initialize(seedSampleRecords);
                SignInAsAdmin();
            }
            catch
            {
                CleanupDirectory();
                throw;
            }
        }

        public string RootPath { get; }
        public string DatabasePath { get; }
        public AppDataService Service { get; }

        public void SignInAsAdmin() => AuthContext.SignIn(new AppUser
        {
            Id = 1,
            UserName = "integration-admin",
            DisplayName = "Integration Admin",
            Role = UserRoles.Admin,
            IsActive = true
        });

        public void SignIn(string role, string displayName) => AuthContext.SignIn(new AppUser
        {
            Id = 999,
            UserName = "integration-user",
            DisplayName = displayName,
            Role = role,
            IsActive = true
        });

        public RecordFormDraft NewRecord(string marker = null)
        {
            marker ??= Guid.NewGuid().ToString("N");
            return new RecordFormDraft
            {
                ReceivedDate = "12/09/2026",
                ReceiveSource = "Trực tiếp",
                ReceiverName = "Integration Admin",
                SenderName = $"Nguyễn Văn Tést {marker}",
                SenderPhone = "0909123456",
                ContactAddress = "Phường Mỹ Bình, An Giang",
                AreaName = "Phường Long Xuyên",
                IncidentAddress = "Đường Trần Hưng Đạo",
                Content = $"Nội dung kiểm thử Unicode 安全 {marker}",
                CaseType = "Khiếu nại",
                ContentGroup = "Đất đai",
                Field = "Hành chính",
                RelatedPerson = "Người liên quan",
                ExpectedHandlingMethod = "Xác minh",
                SenderExpectedHandlingMethod = "Phản hồi bằng văn bản",
                SeverityLevel = "Bình thường",
                ExpectedResultDate = "30/09/2026",
                Note = "Ghi chú thử nghiệm",
                AdditionalNote = "Dữ liệu tổng hợp, không phải dữ liệu thật"
            };
        }

        public void Dispose()
        {
            AuthContext.SignOut();
            Service?.Shutdown();
            CleanupDirectory();
        }

        private void CleanupDirectory()
        {
            var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "QuanLyHoSo.Tests"))
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var resolvedRoot = Path.GetFullPath(RootPath);
            if (!resolvedRoot.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing to clean unexpected test path: {resolvedRoot}");
            }

            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }
    }
}
