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
            ComplaintDate = new DateTime(2026, 8, 10),
            Review = "Nhận xét dòng thứ nhất\nNhận xét dòng thứ hai",
            Proposal = "Đề xuất dòng thứ nhất\nĐề xuất dòng thứ hai",
            CommanderApproverName = "Nguyễn Văn Đội Trưởng",
            ProposingOfficerName = "Lê Văn Cán Bộ",
            LeaderApproverName = "Trần Văn Lãnh Đạo"
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
                Assert.Contains("Nhận xét dòng thứ nhất", text);
                Assert.Contains("Nhận xét dòng thứ hai", text);
                Assert.Contains("Đề xuất dòng thứ nhất", text);
                Assert.Contains("Đề xuất dòng thứ hai", text);
                Assert.NotEmpty(xml.Descendants(w + "br"));
                if (attachment.FileName.StartsWith("phieu_de_xuat", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Contains("Nguyễn Văn Đội Trưởng", text);
                    Assert.Contains("Lê Văn Cán Bộ", text);
                }
                Assert.Contains("Trần Văn Lãnh Đạo", text);
                Assert.DoesNotContain(record.Note, text);
                Assert.DoesNotContain(record.AdditionalNote, text);
                Assert.DoesNotContain("Khưu Quốc Hiếu", text);
                Assert.DoesNotContain("Quách Văn Bền", text);
                Assert.DoesNotContain("Đặng Văn Thinh", text);
                Assert.DoesNotContain("Thượng tá", text);
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
        [InlineData("review")]
        [InlineData("proposal")]
        [InlineData("commander")]
        [InlineData("officer")]
        [InlineData("leader")]
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
            if (missing == "review") details.Review = "  ";
            if (missing == "proposal") details.Proposal = "  ";
            if (missing == "commander") details.CommanderApproverName = "  ";
            if (missing == "officer") details.ProposingOfficerName = "  ";
            if (missing == "leader") details.LeaderApproverName = "  ";
            Assert.Throws<InvalidOperationException>(() => db.Service.UpdateProcessingRecord(code,
                "Kết quả xử lý ban đầu", DateTime.Now, "Integration Admin", "Nội dung", "", "",
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
            Assert.Equal(request.DocumentDetails.Review, restored.DocumentDetails.Review);
            Assert.Equal(request.DocumentDetails.Proposal, restored.DocumentDetails.Proposal);
            Assert.Equal(request.DocumentDetails.CommanderApproverName, restored.DocumentDetails.CommanderApproverName);
            Assert.Equal(request.DocumentDetails.ProposingOfficerName, restored.DocumentDetails.ProposingOfficerName);
            Assert.Equal(request.DocumentDetails.LeaderApproverName, restored.DocumentDetails.LeaderApproverName);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void ProcessingSteps_ShouldFollowEightStepWorkflowOrder()
        {
            using var db = new TestDatabase();
            var code = db.Service.SaveRecordForm(db.NewRecord("nine-step-workflow"));

            db.Service.UpdateProcessingRecord(
                code,
                "Chuyển cơ quan khác",
                DateTime.Now,
                "Integration Admin",
                "Đã được lãnh đạo duyệt và chuyển cơ quan xử lý.",
                string.Empty,
                "Cơ quan kiểm thử",
                Array.Empty<AttachmentDraft>());

            var detail = db.Service.GetProcessingRecordDetail(code);
            Assert.Equal(new[]
            {
                "Tiếp nhận",
                "Phân loại",
                "Phân công",
                "Xác minh",
                "Kết quả xử lý ban đầu",
                "Chuyển cơ quan khác",
                "Chờ kết quả",
                "Lưu hồ sơ"
            }, detail.Steps.Select(step => step.Title));
            Assert.Equal("Chờ kết quả", detail.Status);
            Assert.True(detail.Steps.Single(step => step.Title == "Chuyển cơ quan khác").IsDone);
            Assert.True(detail.Steps.Single(step => step.Title == "Chờ kết quả").IsCurrent);
        }
    }
}
