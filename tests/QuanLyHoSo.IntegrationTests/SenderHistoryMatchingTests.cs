using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class SenderHistoryMatchingTests
    {
        private static RecordFormDraft Draft(TestDatabase db, string senderName, string senderPhone)
        {
            var draft = db.NewRecord("sender-match");
            draft.SenderName = senderName;
            draft.SenderPhone = senderPhone;
            return draft;
        }

        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void MissingPhone_ShouldMatchBySenderNameAndArea()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var row = Assert.Single(db.Service.GetSenderRecords(Draft(db, "Lê Thị Trùng Tên", string.Empty)));

            Assert.Equal(originalCode, row.RecordCode);
            Assert.True(row.IsConfirmedSender);
            Assert.True(row.CanLinkAsResubmission);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void EnteredPhone_ShouldBeNormalizedAndRequiredToMatch()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var match = Assert.Single(db.Service.GetSenderRecords(Draft(db, "Lê Thị Trùng Tên", "+84 912 345 678")));
            Assert.Equal(originalCode, match.RecordCode);
            Assert.Empty(db.Service.GetSenderRecords(Draft(db, "Lê Thị Trùng Tên", "0999 111 222")));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void DifferentNameOrArea_ShouldReturnNothing()
        {
            using var db = new TestDatabase();
            db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            Assert.Empty(db.Service.GetSenderRecords(Draft(db, "Nguyễn Văn Khác", string.Empty)));
            var otherArea = Draft(db, "Lê Thị Trùng Tên", string.Empty);
            otherArea.AreaName = "Phường Mỹ Thới";
            Assert.Empty(db.Service.GetSenderRecords(otherArea));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void ResubmissionValidation_ShouldIgnorePhoneWhenBlankAndIgnoreCaseClassification()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));
            var repeat = Draft(db, "Lê Thị Trùng Tên", string.Empty);
            repeat.CaseType = "Tố cáo";
            repeat.ContentGroup = "Nhóm khác";
            repeat.Field = "Lĩnh vực khác";
            repeat.OriginalRecordCode = originalCode;
            repeat.ResubmissionReason = "Người dùng đã đối chiếu và xác nhận gửi lại.";

            var repeatCode = db.Service.SaveRecordForm(repeat);

            Assert.Equal(originalCode, db.Service.GetRecordForm(repeatCode).OriginalRecordCode);
        }
    }
}
