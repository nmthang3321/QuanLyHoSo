using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class RecordResubmissionTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void RecordDetail_ShouldShowOnlyItsOriginalAndResubmissionsNotIndependentIntakes()
        {
            using var db = new TestDatabase();
            var firstRoot = db.Service.SaveRecordForm(db.NewRecord("separate-cases"));
            Resolve(db, firstRoot);
            var firstRepeat = db.NewRecord("separate-cases");
            firstRepeat.OriginalRecordCode = firstRoot;
            firstRepeat.ResubmissionReason = "Gửi lại vụ việc thứ nhất";
            var firstRepeatCode = db.Service.SaveRecordForm(firstRepeat);

            var secondRoot = db.Service.SaveRecordForm(db.NewRecord("separate-cases"));
            Assert.Equal(secondRoot, Assert.Single(db.Service.GetRecordForm(secondRoot).SenderHistory).RecordCode);
            Resolve(db, secondRoot);
            var secondRepeat = db.NewRecord("separate-cases");
            secondRepeat.OriginalRecordCode = secondRoot;
            secondRepeat.ResubmissionReason = "Gửi lại vụ việc thứ hai";
            var secondRepeatCode = db.Service.SaveRecordForm(secondRepeat);

            foreach (var code in new[] { firstRoot, firstRepeatCode })
                Assert.Equal(new[] { firstRoot, firstRepeatCode }.OrderBy(x => x),
                    db.Service.GetRecordForm(code).SenderHistory.Select(x => x.RecordCode).OrderBy(x => x));
            foreach (var code in new[] { secondRoot, secondRepeatCode })
                Assert.Equal(new[] { secondRoot, secondRepeatCode }.OrderBy(x => x),
                    db.Service.GetRecordForm(code).SenderHistory.Select(x => x.RecordCode).OrderBy(x => x));

            // Intake still needs all sender records for choosing the correct original.
            Assert.Equal(4, db.Service.GetSenderRecords(db.NewRecord("separate-cases")).Count);
        }

        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void Resubmission_ShouldPreserveNewIntakeAndReferenceWithoutCreatingWorkOrResolution()
        {
            using var db = new TestDatabase();
            var original = db.NewRecord("repeat");
            original.ReceivedDate = "01/01/2026";
            var originalCode = db.Service.SaveRecordForm(original);
            Resolve(db, originalCode);
            var totalBefore = db.Service.CountRecords();
            var resolvedBefore = db.Service.GetDashboardMetrics().Single(x => x.Title == "ĐÃ GIẢI QUYẾT").Value;
            var workBefore = db.Service.CountProcessingQueueRecords(null, null, null, null, null);

            var repeat = db.NewRecord("repeat");
            repeat.SenderName = "NGUYEN VAN TEST repeat";
            repeat.SenderPhone = "+84 909 123 456";
            repeat.Content = "Không đồng ý với kết quả trước; gửi lại cùng vụ việc.";
            repeat.OriginalRecordCode = originalCode;
            repeat.ResubmissionReason = "Đã đối chiếu cùng vụ việc, không có tình tiết mới.";
            repeat.Attachments = new[] { new AttachmentDraft { FileName = "gửi-lại.pdf", FileSize = "1 KB", FilePath = @"C:\synthetic\gửi-lại.pdf" } };

            var candidate = Assert.Single(db.Service.GetSenderRecords(repeat));
            Assert.True(candidate.CanLinkAsResubmission);
            Assert.Contains("Kết quả đã giải quyết", candidate.ResolutionSummary);
            var code = db.Service.SaveRecordForm(repeat);
            Assert.NotEqual(originalCode, code);
            var reopened = new AppDataService(db.DatabasePath).GetRecordForm(code);
            Assert.Equal(RecordStatuses.ResubmittedResolved, reopened.Status);
            Assert.Equal(originalCode, reopened.OriginalRecordCode);
            Assert.Equal(repeat.ResubmissionReason, reopened.ResubmissionReason);
            Assert.Equal(repeat.Content, reopened.Content);
            Assert.Equal("12/09/2026", reopened.ReceivedDate);
            Assert.Single(reopened.Attachments);
            Assert.Equal(2, reopened.SenderHistory.Count);
            Assert.Equal(db.Service.GetRecordForm(originalCode).SenderId, reopened.SenderId);
            Assert.Equal(totalBefore + 1, db.Service.CountRecords());
            Assert.Equal(resolvedBefore, db.Service.GetDashboardMetrics().Single(x => x.Title == "ĐÃ GIẢI QUYẾT").Value);
            Assert.Equal(workBefore, db.Service.CountProcessingQueueRecords(null, null, null, null, null));
            Assert.DoesNotContain(db.Service.GetProcessingQueueRecords(null, null, null, null, null, 0, 5000), x => x.RecordCode == code);
            Assert.Throws<InvalidOperationException>(() => db.Service.UpdateProcessingRecord(code, "Đang xác minh", DateTime.Now, "Integration Admin", "", "", "", Array.Empty<AttachmentDraft>()));
            using var connection = new SqliteConnection("Data Source=" + db.DatabasePath);
            connection.Open();
            using var check = connection.CreateCommand();
            check.CommandText = "PRAGMA quick_check;";
            Assert.Equal("ok", check.ExecuteScalar());
            check.CommandText = "SELECT COUNT(*) FROM ProcessHistories WHERE RecordId = (SELECT Id FROM Records WHERE RecordCode = $code);";
            check.Parameters.AddWithValue("$code", code);
            Assert.Equal(0L, check.ExecuteScalar());
        }

        [Theory]
        [InlineData("unresolved")]
        [InlineData("deleted")]
        [InlineData("wrong-sender")]
        [InlineData("wrong-case")]
        [InlineData("no-reason")]
        [Trait("Category", "Regression")]
        [Trait("Category", "Negative")]
        public void InvalidReference_ShouldRejectWithoutSaving(string scenario)
        {
            using var db = new TestDatabase();
            var code = db.Service.SaveRecordForm(db.NewRecord("invalid"));
            if (scenario != "unresolved") Resolve(db, code);
            if (scenario == "deleted") db.Service.DeleteRecord(code);
            var repeat = db.NewRecord("invalid");
            repeat.OriginalRecordCode = code;
            repeat.ResubmissionReason = "Cùng vụ việc";
            if (scenario == "wrong-sender") repeat.SenderPhone = "0909999999";
            if (scenario == "wrong-case") repeat.CaseType = "Tố cáo";
            if (scenario == "no-reason") repeat.ResubmissionReason = " ";
            var before = db.Service.CountRecords();
            Assert.Throws<InvalidOperationException>(() => db.Service.SaveRecordForm(repeat));
            Assert.Equal(before, db.Service.CountRecords());
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void SameNameWithDifferentPhone_ShouldNotMatchAndNewCaseShouldRemainOpen()
        {
            using var db = new TestDatabase();
            var draft = db.NewRecord("identity");
            var firstCode = db.Service.SaveRecordForm(draft);
            Resolve(db, firstCode);
            var other = db.NewRecord("identity");
            other.SenderPhone = "0909999999";
            Assert.Empty(db.Service.GetSenderRecords(other));
            var newCase = db.NewRecord("identity");
            newCase.Content = "Tình tiết mới, cần xử lý riêng";
            var code = db.Service.SaveRecordForm(newCase);
            Assert.Equal("Mới tiếp nhận", db.Service.GetRecordForm(code).Status);
            Assert.Contains(db.Service.GetProcessingQueueRecords(null, null, null, null, null, 0, 5000), x => x.RecordCode == code);
        }

        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Security")]
        public void SenderHistory_ShouldRespectOfficerAccessAndIgnoreForgedSenderId()
        {
            using var db = new TestDatabase();
            var original = db.NewRecord("private");
            var code = db.Service.SaveRecordForm(original);
            original.SenderId = db.Service.GetRecordForm(code).SenderId;
            db.SignIn(UserRoles.Officer, "Other Officer");
            Assert.Empty(db.Service.GetSenderRecords(original));
            db.SignInAsAdmin();
            var forged = db.NewRecord("different-person");
            forged.SenderId = original.SenderId;
            Assert.Empty(db.Service.GetSenderRecords(forged));
        }

        private static void Resolve(TestDatabase db, string code) =>
            db.Service.UpdateProcessingRecord(code, "Đã giải quyết", new DateTime(2026, 2, 1), "Integration Admin", "Kết quả đã giải quyết", "Ghi chú kết quả", "", Array.Empty<AttachmentDraft>());

        [Fact]
        [Trait("Category", "Regression")]
        public void EditingSenderIdentity_ShouldNotKeepAnotherSendersHistory()
        {
            using var db = new TestDatabase();
            var firstCode = db.Service.SaveRecordForm(db.NewRecord("sender-edit"));
            var secondCode = db.Service.SaveRecordForm(db.NewRecord("sender-edit"));
            var originalId = db.Service.GetRecordForm(firstCode).SenderId;
            var edit = db.Service.GetRecordForm(secondCode);
            edit.SenderPhone = "0909999999";
            db.Service.SaveRecordForm(edit, secondCode);
            var saved = db.Service.GetRecordForm(secondCode);
            Assert.NotEqual(originalId, saved.SenderId);
            Assert.Equal(secondCode, Assert.Single(saved.SenderHistory).RecordCode);
            Assert.Equal(firstCode, Assert.Single(db.Service.GetRecordForm(firstCode).SenderHistory).RecordCode);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void LinkedRecords_ShouldKeepReferenceOnEditAndProtectOriginalResult()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(db.NewRecord("protected"));
            Resolve(db, originalCode);
            var repeat = db.NewRecord("protected");
            repeat.OriginalRecordCode = originalCode;
            repeat.ResubmissionReason = "Cùng vụ việc";
            var code = db.Service.SaveRecordForm(repeat);
            var edit = db.Service.GetRecordForm(code);
            edit.OriginalRecordCode = null;
            edit.ResubmissionReason = null;
            edit.Note = "Bổ sung ghi chú";
            db.Service.SaveRecordForm(edit, code);
            Assert.Equal(originalCode, db.Service.GetRecordForm(code).OriginalRecordCode);
            Assert.Equal("Bổ sung ghi chú", db.Service.GetRecordForm(code).Note);
            edit.SenderPhone = "0909999999";
            Assert.Throws<InvalidOperationException>(() => db.Service.SaveRecordForm(edit, code));
            Assert.Throws<InvalidOperationException>(() => db.Service.DeleteRecord(originalCode));
            Assert.Throws<InvalidOperationException>(() => db.Service.UpdateProcessingRecord(originalCode, "Đang xác minh", DateTime.Now, "Integration Admin", "", "", "", Array.Empty<AttachmentDraft>()));
            var original = db.Service.GetRecordForm(originalCode);
            original.RecordCode = "RENAMED";
            Assert.Throws<InvalidOperationException>(() => db.Service.SaveRecordForm(original, originalCode));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void SenderWithoutPhone_ShouldMatchNormalizedNameAndAddressOnly()
        {
            using var db = new TestDatabase();
            var original = db.NewRecord("address");
            original.SenderPhone = "";
            db.Service.SaveRecordForm(original);
            var same = db.NewRecord("address");
            same.SenderPhone = "";
            same.SenderName = "nguyen van test address";
            same.ContactAddress = "  phuong my binh, an giang ";
            Assert.Single(db.Service.GetSenderRecords(same));
            same.ContactAddress = "Địa chỉ khác";
            Assert.Empty(db.Service.GetSenderRecords(same));
            same.ContactAddress = "";
            Assert.Empty(db.Service.GetSenderRecords(same));
        }

        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Database")]
        public void ExistingDatabaseWithoutNewColumns_ShouldMigrateIdempotentlyWithoutLosingRecords()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(db.NewRecord("legacy"));
            var copyPath = System.IO.Path.Combine(db.RootPath, "legacy-copy.db");
            db.Service.BackupDatabase(copyPath);
            using (var connection = new SqliteConnection("Data Source=" + copyPath))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Records';";
                var schema = (string)command.ExecuteScalar();
                schema = System.Text.RegularExpressions.Regex.Replace(schema,
                    @",\s*(SenderId|OriginalRecordCode|ResubmissionReason)\s+TEXT NOT NULL DEFAULT ''", "");
                schema = schema.Replace("CREATE TABLE Records", "CREATE TABLE Records_PreMetadata");
                command.CommandText = "PRAGMA table_info(Records);";
                var columns = new System.Collections.Generic.List<string>();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var name = reader.GetString(1);
                        if (name != "SenderId" && name != "OriginalRecordCode" && name != "ResubmissionReason")
                            columns.Add("\"" + name + "\"");
                    }
                var projection = string.Join(",", columns);
                // The bundled SQLite predates DROP COLUMN; rebuild only the isolated copy.
                command.CommandText = "PRAGMA foreign_keys = OFF;" + schema + ";" +
                    "INSERT INTO Records_PreMetadata (" + projection + ") SELECT " + projection + " FROM Records;" +
                    "DROP TABLE Records; ALTER TABLE Records_PreMetadata RENAME TO Records; PRAGMA foreign_keys = ON;";
                command.ExecuteNonQuery();
            }
            var migrated = new AppDataService(copyPath);
            migrated.Initialize();
            migrated.Initialize();
            var loaded = migrated.GetRecordForm(originalCode);
            Assert.Equal("Nội dung kiểm thử Unicode 安全 legacy", loaded.Content);
            Assert.Equal("", loaded.OriginalRecordCode);
            Assert.Single(migrated.GetSenderRecords(loaded));
            using var checkConnection = new SqliteConnection("Data Source=" + copyPath);
            checkConnection.Open();
            using var check = checkConnection.CreateCommand();
            check.CommandText = "PRAGMA quick_check;";
            Assert.Equal("ok", check.ExecuteScalar());
        }

        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Integration")]
        public void LanRoutes_ShouldRequireSessionAndPreserveResubmissionAndHistory()
        {
            using var db = new TestDatabase();
            var code = db.Service.SaveRecordForm(db.NewRecord("lan"));
            Resolve(db, code);
            var portProbe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((System.Net.IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();
            AppPathSettings.UseServerMode(db.DatabasePath, System.IO.Path.Combine(db.RootPath, "logs"), "http://127.0.0.1:" + port);
            var server = new LanDataServer(db.Service);
            server.Start();
            try
            {
                using var client = new LanDataClient();
                AuthContext.SignOut();
                Assert.Throws<InvalidOperationException>(() => client.Call<SenderRecordHistory[]>("records/sender-history", db.NewRecord("lan")));
                var user = client.Call<AppUser>("auth/login", new LoginRequest { UserName = "admin", Password = "admin123" });
                AuthContext.SignIn(user);
                var repeat = db.NewRecord("lan");
                repeat.OriginalRecordCode = code;
                repeat.ResubmissionReason = "Đã đối chiếu";
                Assert.Single(client.Call<SenderRecordHistory[]>("records/sender-history", repeat));
                var saved = client.Call<string>("records/save-resubmission", new SaveRecordFormRequest { Record = repeat });
                var detail = client.Call<RecordFormDraft>("records/detail", new RecordCodeRequest { RecordCode = saved });
                Assert.Equal(RecordStatuses.ResubmittedResolved, detail.Status);
                Assert.Equal(code, detail.OriginalRecordCode);
                Assert.Equal(2, detail.SenderHistory.Count);
                Assert.Throws<InvalidOperationException>(() => client.Call<bool>("processing/update", new UpdateProcessingRequest
                {
                    RecordCode = saved, Status = "Đang xác minh", ProcessorName = "Integration Admin", ProcessedAt = DateTime.Now,
                    Attachments = Array.Empty<AttachmentDraft>()
                }));
            }
            finally { server.Stop(); }
        }
    }
}
