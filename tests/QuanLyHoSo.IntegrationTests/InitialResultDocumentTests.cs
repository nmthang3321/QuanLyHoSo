using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using QuanLyHoSo.Infrastructure.Documents;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class InitialResultDocumentTests
    {
        private static InitialResultDocumentDetails Details() => new InitialResultDocumentDetails
        {
            TransferNumber = "  456/PC-VKS-TTra  ",
            TransferDate = new DateTime(2026, 8, 15),
            ComplaintDate = new DateTime(2026, 8, 10)
        };

        [Fact]
        [Trait("Category", "Regression")]
        public void Generate_ShouldFillTransferFieldsInAllThreeTemplatesAndPreserveFormatting()
        {
            using var db = new TestDatabase();
            var record = db.NewRecord("document-fields");
            record.Note = "Nhận xét tổng hợp";
            record.AdditionalNote = "Đề xuất xử lý";
            var generated = InitialResultDocumentGenerator.Generate(record, "HS-DOC-456", "20/08/2026", db.RootPath, documentDetails: Details());
            Assert.Equal(3, generated.Count);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            foreach (var attachment in generated)
            {
                using var archive = ZipFile.OpenRead(attachment.FilePath);
                using var stream = archive.GetEntry("word/document.xml").Open();
                var xml = XDocument.Load(stream);
                var text = string.Concat(xml.Descendants(w + "t").Select(t => t.Value));
                Assert.Contains("Phiếu chuyển đơn số 456/PC-VKS-TTra ngày 15/08/2026", text);
                Assert.Contains("Đơn tố cáo đề ngày 10/08/2026", text);
                Assert.Contains(record.SenderName, text);
                Assert.Contains(record.ContactAddress, text);
                Assert.Contains(record.Content, text);
                Assert.Contains(record.Note, text);
                Assert.DoesNotContain("310/PC-VKS-TTra", text);
                Assert.Empty(xml.Descendants(w + "highlight"));
                Assert.NotEmpty(xml.Descendants(w + "rPr"));
            }
            Assert.Empty(InitialResultDocumentGenerator.Generate(record, "HS-DOC-456", "20/08/2026", db.RootPath, generated, Details()));
        }

        [Theory]
        [InlineData("number")]
        [InlineData("transfer-date")]
        [InlineData("complaint-date")]
        [Trait("Category", "Regression")]
        public void Update_ShouldRejectMissingTransferFieldsWithoutChangingRecord(string missing)
        {
            using var db = new TestDatabase();
            var code = db.Service.SaveRecordForm(db.NewRecord());
            var original = db.Service.GetProcessingRecordDetail(code);
            var details = Details();
            if (missing == "number") details.TransferNumber = "  ";
            if (missing == "transfer-date") details.TransferDate = null;
            if (missing == "complaint-date") details.ComplaintDate = null;
            Assert.Throws<InvalidOperationException>(() => db.Service.UpdateProcessingRecord(code,
                "Đang chờ bổ sung tài liệu", DateTime.Now, "Integration Admin", "Nội dung", "", "",
                Array.Empty<AttachmentDraft>(), true, details));
            Assert.Equal(original.Status, db.Service.GetProcessingRecordDetail(code).Status);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void TransferDetails_ShouldRoundTripThroughLanRequest()
        {
            var request = new UpdateProcessingRequest { GenerateInitialResultDocuments = true, DocumentDetails = Details() };
            var restored = JsonSerializer.Deserialize<UpdateProcessingRequest>(JsonSerializer.Serialize(request));
            Assert.Equal(request.DocumentDetails.TransferNumber, restored.DocumentDetails.TransferNumber);
            Assert.Equal(request.DocumentDetails.TransferDate, restored.DocumentDetails.TransferDate);
            Assert.Equal(request.DocumentDetails.ComplaintDate, restored.DocumentDetails.ComplaintDate);
        }
    }
}
