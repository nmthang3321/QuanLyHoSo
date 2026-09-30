using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using QuanLyHoSo.Infrastructure.Data;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    // Restore requirements: the built-in admin keeps the credentials in use
    // before the restore, and the restored database is migrated/normalized to
    // the current schema immediately — a restore never depends on a later
    // server restart to become usable.
    public sealed class RestoreBehaviorTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void RestoringOldDatabase_ShouldKeepCurrentAdminPasswordAndRestoredRecords()
        {
            using var db = new TestDatabase();
            var recordCode = db.Service.SaveRecordForm(db.NewRecord("restore-admin"));
            var backupPath = Path.Combine(db.RootPath, "before-password-change.db");
            db.Service.BackupDatabase(backupPath);
            var backupBytes = File.ReadAllBytes(backupPath);

            Assert.True(db.Service.ChangeCurrentUserPassword("admin123", "MatKhauMoi123"));
            Assert.NotNull(db.Service.AuthenticateUser("admin", "MatKhauMoi123"));

            db.Service.RestoreDatabaseFromUpload("before-password-change.db", backupBytes);

            // The current password still works; the backup's old password does not.
            Assert.NotNull(db.Service.AuthenticateUser("admin", "MatKhauMoi123"));
            Assert.Null(db.Service.AuthenticateUser("admin", "admin123"));
            // The restored records are present.
            Assert.Equal("Nội dung kiểm thử Unicode 安全 restore-admin", db.Service.GetRecordForm(recordCode).Content);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void RestoringLegacyDatabase_ShouldMigrateSchemaImmediatelyWithoutLosingRecords()
        {
            using var db = new TestDatabase();
            var recordCode = db.Service.SaveRecordForm(db.NewRecord("restore-legacy"));
            var backupPath = Path.Combine(db.RootPath, "legacy.db");
            db.Service.BackupDatabase(backupPath);
            StripDocumentColumns(backupPath);
            using (var probe = new SqliteConnection("Data Source=" + backupPath))
            {
                probe.Open();
                var names = new System.Collections.Generic.List<string>();
                using (var command = probe.CreateCommand())
                {
                    command.CommandText = "PRAGMA table_info(Records);";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        names.Add(reader.GetString(1));
                    }
                }

                Assert.DoesNotContain("CommanderApproverName", names);
            }

            db.Service.RestoreDatabaseFromUpload("legacy.db", File.ReadAllBytes(backupPath));

            // The record-list query runs against the restored legacy schema with
            // no server restart in between.
            var rows = db.Service.GetFilteredRecords(null, null, null, null, null, null, null, null, null, 100, 0);
            Assert.Contains(rows, row => row.RecordCode == recordCode);
            Assert.NotNull(db.Service.AuthenticateUser("admin", "admin123"));
        }

        private static void StripDocumentColumns(string databasePath)
        {
            using var connection = new SqliteConnection("Data Source=" + databasePath);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Records';";
            var schema = (string)command.ExecuteScalar();
            schema = Regex.Replace(schema,
                @",\s*(CommanderApproverName|LeaderApproverName|TransferDocumentNumber|TransferDocumentDate)\s+TEXT NOT NULL DEFAULT ''", "");
            schema = schema.Replace("CREATE TABLE Records", "CREATE TABLE Records_PreDocument");
            var columns = new System.Collections.Generic.List<string>();
            command.CommandText = "PRAGMA table_info(Records);";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(1);
                    if (name != "CommanderApproverName" && name != "LeaderApproverName"
                        && name != "TransferDocumentNumber" && name != "TransferDocumentDate")
                    {
                        columns.Add("\"" + name + "\"");
                    }
                }
            }

            var projection = string.Join(",", columns);
            // The bundled SQLite predates DROP COLUMN; rebuild only the isolated copy.
            command.CommandText = "PRAGMA foreign_keys = OFF;" + schema + ";" +
                "INSERT INTO Records_PreDocument (" + projection + ") SELECT " + projection + " FROM Records;" +
                "DROP TABLE Records; ALTER TABLE Records_PreDocument RENAME TO Records; PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
        }
    }
}
