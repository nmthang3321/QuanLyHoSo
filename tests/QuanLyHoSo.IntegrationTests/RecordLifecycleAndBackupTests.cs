using System;
using System.IO;
using System.Linq;
using QuanLyHoSo.Infrastructure.Data;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class RecordLifecycleAndBackupTests
    {
        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Database")]
        public void DeleteAndRestore_WhenBatchMatches_ShouldRecoverRecordAndRejectStaleUndo()
        {
            using var database = new TestDatabase();
            var code = database.Service.SaveRecordForm(database.NewRecord("restore"));
            const string batch = "batch-current";

            Assert.True(database.Service.DeleteRecord(code, batch));
            Assert.True(string.IsNullOrEmpty(database.Service.GetRecordForm(code).RecordCode));
            Assert.False(database.Service.RestoreRecord(code, "stale-batch"));
            Assert.True(database.Service.RestoreRecord(code, batch));
            Assert.Equal(code, database.Service.GetRecordForm(code).RecordCode);
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Database")]
        public void PermanentDelete_WhenBatchMatches_ShouldRemoveRecordAndPreventRestore()
        {
            using var database = new TestDatabase();
            var code = database.Service.SaveRecordForm(database.NewRecord("permanent"));
            const string batch = "permanent-batch";
            database.Service.DeleteRecord(code, batch);

            Assert.True(database.Service.PermanentlyDeleteRecord(code, batch));
            Assert.DoesNotContain(database.Service.GetDeletedRecords(), item => item.RecordCode == code);
            Assert.False(database.Service.RestoreRecord(code, batch));
            Assert.True(string.IsNullOrEmpty(database.Service.GetRecordForm(code).RecordCode));
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Backup")]
        public void BackupModifyRestore_ShouldRestoreOriginalRecordState()
        {
            using var database = new TestDatabase();
            var code = database.Service.SaveRecordForm(database.NewRecord("before-backup"));
            var backupPath = Path.Combine(database.RootPath, "exports", "backup.db");
            var safetyPath = Path.Combine(database.RootPath, "exports", "safety.db");
            database.Service.BackupDatabase(backupPath);
            var modified = database.Service.GetRecordForm(code);
            modified.Content = "changed after backup";
            database.Service.SaveRecordForm(modified, code);

            database.Service.RestoreDatabaseFromFile(backupPath, safetyPath);

            Assert.Equal("Nội dung kiểm thử Unicode 安全 before-backup", database.Service.GetRecordForm(code).Content);
            Assert.True(File.Exists(safetyPath));
            AppDataService.ValidateDatabaseFile(safetyPath);
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Backup")]
        [Trait("Category", "Regression")]
        public void Restore_WhenBackupIsCorrupted_ShouldFailBeforeChangingCurrentDatabase()
        {
            using var database = new TestDatabase();
            var code = database.Service.SaveRecordForm(database.NewRecord("corrupt-guard"));
            var corruptPath = Path.Combine(database.RootPath, "corrupt.db");
            var safetyPath = Path.Combine(database.RootPath, "should-not-exist.db");
            File.WriteAllBytes(corruptPath, new byte[] { 1, 2, 3, 4, 5, 6 });

            Assert.ThrowsAny<Exception>(() => database.Service.RestoreDatabaseFromFile(corruptPath, safetyPath));

            Assert.Equal(code, database.Service.GetRecordForm(code).RecordCode);
            Assert.False(File.Exists(safetyPath));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("Category", "Backup")]
        [Trait("Category", "Negative")]
        public void BackupDatabase_WhenDestinationIsMissing_ShouldRejectInput(string destination)
        {
            using var database = new TestDatabase();

            Assert.Throws<ArgumentException>(() => database.Service.BackupDatabase(destination));
        }
    }
}
