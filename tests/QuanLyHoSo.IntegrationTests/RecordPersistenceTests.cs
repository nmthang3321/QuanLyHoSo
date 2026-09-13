using System;
using System.Linq;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class RecordPersistenceTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Critical")]
        [Trait("Category", "Database")]
        public void SaveReadUpdateReopen_WhenRecordContainsUnicode_ShouldPreserveAllImportantValues()
        {
            using var database = new TestDatabase();
            var draft = database.NewRecord("persistence");
            draft.Attachments = new[]
            {
                new AttachmentDraft { FileName = "tài-liệu.pdf", FileSize = "12 KB", FilePath = @"C:\synthetic\tài-liệu.pdf" }
            };

            var code = database.Service.SaveRecordForm(draft);
            var created = database.Service.GetRecordForm(code);
            Assert.Equal("Nguyễn Văn Tést persistence", created.SenderName);
            Assert.Equal("Nội dung kiểm thử Unicode 安全 persistence", created.Content);
            Assert.Equal("tài-liệu.pdf", Assert.Single(created.Attachments).FileName);

            created.Content = "Nội dung đã cập nhật Ω";
            created.Note = "Ghi chú sau cập nhật";
            Assert.Equal(code, database.Service.SaveRecordForm(created, code));

            var reopened = new AppDataService(database.DatabasePath);
            var persisted = reopened.GetRecordForm(code);
            Assert.Equal("Nội dung đã cập nhật Ω", persisted.Content);
            Assert.Equal("Ghi chú sau cập nhật", persisted.Note);
            Assert.Equal(code, persisted.RecordCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Search")]
        public void Search_WhenUsingExactPartialAndCaseInsensitiveText_ShouldReturnOnlyMatchingRecord()
        {
            using var database = new TestDatabase();
            var marker = "UniqueSearch" + Guid.NewGuid().ToString("N");
            var draft = database.NewRecord(marker);
            var code = database.Service.SaveRecordForm(draft);

            var exact = Search(database, draft.SenderName);
            var partial = Search(database, marker.Substring(0, 14).ToLowerInvariant());
            var missing = Search(database, "definitely-not-present-安全");

            Assert.Contains(exact, item => item.RecordCode == code);
            Assert.Contains(partial, item => item.RecordCode == code);
            Assert.Empty(missing);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Pagination")]
        public void Pagination_WhenPageSizeIsOne_ShouldReturnDistinctConsecutiveRecords()
        {
            using var database = new TestDatabase();
            database.Service.SaveRecordForm(database.NewRecord("page-a"));
            database.Service.SaveRecordForm(database.NewRecord("page-b"));

            var first = database.Service.GetFilteredRecords(null, null, null, null, null, null, null, null, "Mới nhất", 1, 0);
            var second = database.Service.GetFilteredRecords(null, null, null, null, null, null, null, null, "Mới nhất", 1, 1);

            Assert.Single(first);
            Assert.Single(second);
            Assert.NotEqual(first[0].RecordCode, second[0].RecordCode);
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Regression")]
        public void Update_WhenRecordCodeConflicts_ShouldRollbackWithoutDamagingEitherRecord()
        {
            using var database = new TestDatabase();
            var firstCode = database.Service.SaveRecordForm(database.NewRecord("conflict-a"));
            var secondCode = database.Service.SaveRecordForm(database.NewRecord("conflict-b"));
            var second = database.Service.GetRecordForm(secondCode);
            second.RecordCode = firstCode;
            second.Content = "must roll back";

            Assert.ThrowsAny<Exception>(() => database.Service.SaveRecordForm(second, secondCode));

            Assert.Equal("Nội dung kiểm thử Unicode 安全 conflict-a", database.Service.GetRecordForm(firstCode).Content);
            Assert.Equal("Nội dung kiểm thử Unicode 安全 conflict-b", database.Service.GetRecordForm(secondCode).Content);
        }

        [Fact]
        [Trait("Category", "Security")]
        [Trait("Category", "Critical")]
        public void CreateRecord_WhenRoleIsNotAdmin_ShouldBeDeniedByDataLayer()
        {
            using var database = new TestDatabase();
            database.SignIn(UserRoles.Leader, "Lãnh đạo");

            Assert.Throws<UnauthorizedAccessException>(() => database.Service.SaveRecordForm(database.NewRecord("denied")));
        }

        [Fact]
        [Trait("Category", "Security")]
        [Trait("Category", "Integration")]
        public void ReadRecord_WhenOfficerIsNotAssigned_ShouldNotExposeRecordDetails()
        {
            using var database = new TestDatabase();
            var code = database.Service.SaveRecordForm(database.NewRecord("private"));
            database.SignIn(UserRoles.Officer, "Một cán bộ khác");

            var result = database.Service.GetRecordForm(code);

            Assert.True(string.IsNullOrEmpty(result.RecordCode));
            Assert.True(string.IsNullOrEmpty(result.Content));
        }

        private static System.Collections.Generic.IReadOnlyList<RecentRecord> Search(TestDatabase database, string text) =>
            database.Service.GetFilteredRecords(null, null, null, null, null, null, null, text, "Mới nhất", 100, 0);
    }
}
