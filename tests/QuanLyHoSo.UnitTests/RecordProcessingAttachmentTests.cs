using System;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class RecordProcessingAttachmentTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        public void OpenRecord_WhenProcessingPayloadHasNoAttachments_ShouldUseRecordAttachments()
        {
            var attachment = new AttachmentDraft
            {
                FileName = "tai-lieu.pdf",
                FileSize = "12 KB",
                FilePath = @"C:\synthetic\tai-lieu.pdf"
            };
            var service = CreateService();
            service.Setup(x => x.GetProcessingRecordDetail("HS-ATTACHMENT"))
                .Returns(new ProcessingRecordDetail
                {
                    RecordCode = "HS-ATTACHMENT",
                    Status = "Đang xác minh",
                    Attachments = Array.Empty<AttachmentDraft>()
                });
            service.Setup(x => x.GetRecordForm("HS-ATTACHMENT"))
                .Returns(new RecordFormDraft
                {
                    RecordCode = "HS-ATTACHMENT",
                    Attachments = new[] { attachment }
                });

            using var viewModel = new RecordProcessingViewModel(service.Object);
            viewModel.OpenRecord("HS-ATTACHMENT");

            Assert.True(viewModel.HasAttachments);
            Assert.Same(attachment, Assert.Single(viewModel.Attachments));
            Assert.Same(attachment, Assert.Single(viewModel.SelectedProcessingDetail.Attachments));
        }

        private static Mock<IApplicationDataService> CreateService()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetProcessingQueueMetrics()).Returns(Array.Empty<DashboardMetric>());
            service.Setup(x => x.GetProcessingQueueRecords(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Array.Empty<ProcessingQueueRecord>());
            service.Setup(x => x.CountProcessingQueueRecords(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(0);
            return service;
        }
    }
}
