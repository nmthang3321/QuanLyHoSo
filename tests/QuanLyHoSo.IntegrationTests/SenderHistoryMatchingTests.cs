using System;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    // Duplicate complaint detection: a sender name alone must surface the
    // comparison dialog as a warning, while linking stays gated on the strict
    // identity rules (name + phone, or name + address when both phones are empty).
    public sealed class SenderHistoryMatchingTests
    {
        private static RecordFormDraft Draft(TestDatabase db, string senderName, string senderPhone, string contactAddress = "Phường Mỹ Bình, An Giang")
        {
            var draft = db.NewRecord("sender-match");
            draft.SenderName = senderName;
            draft.SenderPhone = senderPhone;
            draft.ContactAddress = contactAddress;
            return draft;
        }

        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void SameNameWithoutPhone_ShouldSurfaceUnconfirmedWarningRow()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var nameOnly = Draft(db, "Lê Thị Trùng Tên", string.Empty, contactAddress: string.Empty);
            var row = Assert.Single(db.Service.GetSenderRecords(nameOnly));

            Assert.Equal(originalCode, row.RecordCode);
            Assert.False(row.IsConfirmedSender);
            Assert.False(row.CanLinkAsResubmission);
            Assert.Contains("chưa xác thực", row.SenderMatchDisplay);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void SameNameWithMatchingPhone_ShouldConfirmAndAllowLinking()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var match = Assert.Single(db.Service.GetSenderRecords(Draft(db, "Lê Thị Trùng Tên", "0912345678")));

            Assert.Equal(originalCode, match.RecordCode);
            Assert.True(match.IsConfirmedSender);
            Assert.True(match.IsSameCase);
            Assert.True(match.CanLinkAsResubmission);
            Assert.Equal("Đã xác thực", match.SenderMatchDisplay);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void SameNameWithDifferentPhone_ShouldWarnNotConfirm()
        {
            using var db = new TestDatabase();
            db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var row = Assert.Single(db.Service.GetSenderRecords(Draft(db, "Lê Thị Trùng Tên", "0999 111 222")));

            Assert.False(row.IsConfirmedSender);
            Assert.False(row.CanLinkAsResubmission);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void DifferentName_ShouldReturnNothing()
        {
            using var db = new TestDatabase();
            db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            Assert.Empty(db.Service.GetSenderRecords(Draft(db, "Nguyễn Văn Khác", "0912 345 678")));
            Assert.Empty(db.Service.GetSenderRecords(Draft(db, "Lê Thị Khác Tên", string.Empty, contactAddress: string.Empty)));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void SavingResubmissionAgainstUnconfirmedRow_ShouldBeRejectedServerSide()
        {
            using var db = new TestDatabase();
            var originalCode = db.Service.SaveRecordForm(Draft(db, "Lê Thị Trùng Tên", "0912 345 678"));

            var unconfirmed = Draft(db, "Lê Thị Trùng Tên", string.Empty, contactAddress: string.Empty);
            unconfirmed.OriginalRecordCode = originalCode;
            unconfirmed.ResubmissionReason = "Trùng tên nên thử liên kết dù chưa xác thực.";

            Assert.Throws<InvalidOperationException>(() => db.Service.SaveRecordForm(unconfirmed));
        }
    }
}
