using System;
using System.IO;
using System.Linq;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;
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

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Attachments")]
        public void SaveRecord_WithAttachmentContent_ShouldStorePhysicalFileOnServer()
        {
            using var database = new TestDatabase();
            var sourcePath = Path.Combine(database.RootPath, "client-source.txt");
            var expectedContent = new byte[] { 10, 20, 30, 40, 50 };
            File.WriteAllBytes(sourcePath, expectedContent);
            var draft = database.NewRecord("central-attachment");
            draft.Attachments = new[]
            {
                new AttachmentDraft
                {
                    FileName = "evidence.txt",
                    FileSize = "5 B",
                    FilePath = sourcePath,
                    Content = expectedContent
                }
            };

            var code = database.Service.SaveRecordForm(draft);
            File.Delete(sourcePath);
            var storedAttachment = Assert.Single(database.Service.GetRecordForm(code).Attachments);

            Assert.Contains("QuanLyHoSoFiles", storedAttachment.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(storedAttachment.FilePath));
            Assert.Equal(expectedContent, File.ReadAllBytes(storedAttachment.FilePath));
            Assert.Equal(storedAttachment.FilePath, database.Service.GetAttachmentFilePath(code, storedAttachment.FileName));
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Attachments")]
        public void UpdateAttachmentContent_ShouldReplaceServerFileAndUpdateMetadata()
        {
            using var database = new TestDatabase();
            var originalContent = new byte[] { 1, 2, 3 };
            var updatedContent = Enumerable.Range(0, 4096).Select(value => (byte)(value % 251)).ToArray();
            var draft = database.NewRecord("edit-attachment");
            draft.Attachments = new[]
            {
                new AttachmentDraft
                {
                    FileName = "bien-ban.docx",
                    FileSize = "1 KB",
                    Content = originalContent
                }
            };
            var code = database.Service.SaveRecordForm(draft);

            var updated = database.Service.UpdateAttachmentContent(code, "bien-ban.docx", updatedContent);
            var persisted = Assert.Single(database.Service.GetRecordForm(code).Attachments);

            Assert.Equal("4 KB", updated.FileSize);
            Assert.Equal(updated.FileSize, persisted.FileSize);
            Assert.Equal(updated.FilePath, persisted.FilePath);
            Assert.Equal(updatedContent, File.ReadAllBytes(persisted.FilePath));
        }

        [Fact]
        [Trait("Category", "Security")]
        [Trait("Category", "Attachments")]
        public void UpdateAttachmentContent_WhenLeader_ShouldBeDeniedAndKeepOriginalFile()
        {
            using var database = new TestDatabase();
            var originalContent = new byte[] { 8, 6, 4, 2 };
            var draft = database.NewRecord("edit-attachment-denied");
            draft.Attachments = new[]
            {
                new AttachmentDraft
                {
                    FileName = "quyet-dinh.pdf",
                    FileSize = "1 KB",
                    Content = originalContent
                }
            };
            var code = database.Service.SaveRecordForm(draft);
            var storedPath = Assert.Single(database.Service.GetRecordForm(code).Attachments).FilePath;
            database.SignIn(UserRoles.Leader, "Lãnh đạo");

            Assert.Throws<UnauthorizedAccessException>(() => database.Service.UpdateAttachmentContent(code, "quyet-dinh.pdf", new byte[] { 9, 9, 9 }));
            Assert.Equal(originalContent, File.ReadAllBytes(storedPath));
        }

        [Fact]
        [Trait("Category", "Critical")]
        [Trait("Category", "Backup")]
        public void FullBackupRestore_ShouldRecoverDatabaseAndPhysicalAttachments()
        {
            using var database = new TestDatabase();
            var originalBytes = new byte[] { 1, 3, 5, 7, 9 };
            var draft = database.NewRecord("full-backup");
            var originalContent = draft.Content;
            draft.Attachments = new[]
            {
                new AttachmentDraft
                {
                    FileName = "full-backup.txt",
                    FileSize = "5 B",
                    FilePath = Path.Combine(database.RootPath, "client-full-backup.txt"),
                    Content = originalBytes
                }
            };
            var code = database.Service.SaveRecordForm(draft);
            var storedPath = Assert.Single(database.Service.GetRecordForm(code).Attachments).FilePath;
            var packagePath = Path.Combine(database.RootPath, "exports", "complete.qlhbackup");
            var safetyPath = Path.Combine(database.RootPath, "exports", "before-restore.qlhbackup");
            database.Service.BackupApplicationData(packagePath);

            var modified = database.Service.GetRecordForm(code);
            modified.Content = "changed after full backup";
            database.Service.SaveRecordForm(modified, code);
            File.WriteAllBytes(storedPath, new byte[] { 99 });

            database.Service.RestoreApplicationDataPackage(packagePath, safetyPath);

            var restored = database.Service.GetRecordForm(code);
            var restoredAttachment = Assert.Single(restored.Attachments);
            Assert.Equal(originalContent, restored.Content);
            Assert.Equal(originalBytes, File.ReadAllBytes(restoredAttachment.FilePath));
            Assert.True(File.Exists(safetyPath));
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

        [Fact]
        [Trait("Category", "Backup")]
        [Trait("Category", "Regression")]
        public void AutomaticBackup_ShouldRunEverySevenDaysAndKeepOnlyTenNewestFiles()
        {
            using var database = new TestDatabase();
            var backupFolder = Path.Combine(database.RootPath, "automatic-backups");
            var firstCheck = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            Directory.CreateDirectory(backupFolder);
            var legacyBackup = Path.Combine(backupFolder, "quanlyhoso_backup_20251201_080000.db");
            var safetyBackup = Path.Combine(backupFolder, "quanlyhoso_before_restore_20251208_080000.db");
            File.WriteAllText(legacyBackup, "legacy");
            File.WriteAllText(safetyBackup, "safety");
            File.SetLastWriteTimeUtc(legacyBackup, firstCheck.AddDays(-31));
            File.SetLastWriteTimeUtc(safetyBackup, firstCheck.AddDays(-24));

            var firstBackup = database.Service.CreateAutomaticBackupIfDue(firstCheck, backupFolder);
            var earlyBackup = database.Service.CreateAutomaticBackupIfDue(firstCheck.AddDays(6), backupFolder);

            Assert.True(File.Exists(firstBackup));
            Assert.Null(earlyBackup);

            for (var index = 1; index <= 11; index++)
            {
                var createdPath = database.Service.CreateAutomaticBackupIfDue(firstCheck.AddDays(index * 7), backupFolder);
                Assert.True(File.Exists(createdPath));
            }

            var retainedBackups = Directory.GetFiles(backupFolder, "quanlyhoso_*.*");
            Assert.Equal(10, retainedBackups.Length);
            Assert.All(retainedBackups, path => Assert.Equal(".qlhbackup", Path.GetExtension(path)));
            Assert.DoesNotContain(firstBackup, retainedBackups);
            Assert.DoesNotContain(legacyBackup, retainedBackups);
            Assert.DoesNotContain(safetyBackup, retainedBackups);
            Assert.DoesNotContain(retainedBackups, path => Path.GetFileName(path).Contains("20260108"));
        }
    }
}
