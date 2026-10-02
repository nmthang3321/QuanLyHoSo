using System;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class RecordResubmissionViewModelTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void EarlyWarning_ShouldOpenPopupAndStageRepeatUntilMainSave()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            var original = new SenderRecordHistory { RecordCode = "original", Status = "Đã giải quyết", IsSameCase = true };
            service.Setup(x => x.GetSenderRecords(It.IsAny<RecordFormDraft>())).Returns(new[]
            {
                new SenderRecordHistory { RecordCode = "latest-repeat", Status = RecordStatuses.ResubmittedResolved, OriginalRecordCode = "original", IsSameCase = true },
                original
            });
            RecordFormDraft saved = null;
            service.Setup(x => x.SaveRecordForm(It.IsAny<RecordFormDraft>(), null))
                .Callback<RecordFormDraft, string>((draft, code) => saved = draft).Returns("new-repeat");
            using var vm = NewFilledForm(service);
            vm.RefreshPotentialDuplicateCheck();
            Assert.True(vm.HasPotentialDuplicate);
            vm.OpenDetectedSenderHistoryCommand.Execute(null);
            Assert.True(vm.IsSenderHistoryOpen);
            Assert.Same(original, vm.SelectedSenderRecord);
            vm.SaveResubmissionCommand.Execute(null);
            Assert.Contains("lý do", vm.SenderHistoryError);
            Assert.Null(saved);
            vm.ResubmissionReason = "  Cùng vụ việc, đã đối chiếu  ";
            vm.SaveResubmissionCommand.Execute(null);
            Assert.Null(saved);
            Assert.False(vm.IsSenderHistoryOpen);
            Assert.Contains("original", vm.DuplicateSenderNotice);
            vm.SaveCommand.Execute(null);
            Assert.Equal("original", saved.OriginalRecordCode);
            Assert.Equal("Cùng vụ việc, đã đối chiếu", saved.ResubmissionReason);
            Assert.Equal("Test Admin", saved.ReceiverName);
            Assert.True(vm.IsResubmission);
            Assert.Equal("new-repeat", vm.RecordCode);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void Popup_ShouldEnableConfirmationOnlyForOriginalRow()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            var unresolved = new SenderRecordHistory { RecordCode = "unresolved", Status = "Đang xác minh", IsSameCase = true };
            var repeat = new SenderRecordHistory { RecordCode = "repeat", OriginalRecordCode = "unresolved", Status = RecordStatuses.ResubmittedResolved, IsSameCase = true };
            service.Setup(x => x.GetSenderRecords(It.IsAny<RecordFormDraft>())).Returns(new[] { repeat, unresolved });
            using var vm = NewFilledForm(service);
            vm.RefreshPotentialDuplicateCheck();
            vm.OpenDetectedSenderHistoryCommand.Execute(null);
            Assert.True(vm.IsSenderHistoryOpen);
            Assert.Same(unresolved, vm.SelectedSenderRecord);
            Assert.True(vm.SaveResubmissionCommand.CanExecute(null));
            vm.SelectedSenderRecord = repeat;
            Assert.False(vm.SaveResubmissionCommand.CanExecute(null));
            vm.SelectedSenderRecord = unresolved;
            Assert.True(vm.SaveResubmissionCommand.CanExecute(null));
            vm.CloseSenderHistoryCommand.Execute(null);
            Assert.False(vm.IsSenderHistoryOpen);
            Assert.Equal("Nội dung mới", vm.Content);
            service.Verify(x => x.SaveRecordForm(It.IsAny<RecordFormDraft>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void IgnoringEarlyWarning_ShouldSaveOrdinaryIntakeWithoutReference()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            service.Setup(x => x.GetSenderRecords(It.IsAny<RecordFormDraft>())).Returns(new[]
                { new SenderRecordHistory { RecordCode = "original", Status = "Đã giải quyết", IsSameCase = true } });
            RecordFormDraft saved = null;
            service.Setup(x => x.SaveRecordForm(It.IsAny<RecordFormDraft>(), null))
                .Callback<RecordFormDraft, string>((draft, code) => saved = draft).Returns("ordinary");
            using var vm = NewFilledForm(service);
            vm.RefreshPotentialDuplicateCheck();
            Assert.True(vm.HasPotentialDuplicate);
            vm.SaveCommand.Execute(null);
            Assert.Null(saved.OriginalRecordCode);
            Assert.False(vm.IsResubmission);
            Assert.False(vm.IsSenderHistoryOpen);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void ChangingDuplicateKey_ShouldImmediatelyHideWarningAndClearStagedLink()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            var original = new SenderRecordHistory { RecordCode = "original", Status = "Đang xác minh", IsSameCase = true };
            service.Setup(x => x.GetSenderRecords(It.IsAny<RecordFormDraft>())).Returns(new[] { original });
            using var vm = NewFilledForm(service);
            vm.RefreshPotentialDuplicateCheck();
            vm.OpenDetectedSenderHistoryCommand.Execute(null);
            vm.ResubmissionReason = "Đã đối chiếu";
            vm.SaveResubmissionCommand.Execute(null);
            Assert.Contains("original", vm.DuplicateSenderNotice);

            vm.AreaName = "Phường Mỹ Thới";

            Assert.False(vm.HasPotentialDuplicate);
            Assert.DoesNotContain("original", vm.DuplicateSenderNotice);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void DuplicateRecordCode_ShouldWarnAndBlockSave()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            service.Setup(x => x.GetRecordForm("HS-2026-000099"))
                .Returns(new RecordFormDraft { RecordCode = "HS-2026-000099", Status = "Đang xác minh" });
            using var vm = NewFilledForm(service);

            vm.RecordCode = "HS-2026-000099";
            vm.RefreshRecordCodeDuplicateCheck();
            Assert.True(vm.HasDuplicateRecordCode);
            Assert.Contains("đã tồn tại", vm.RecordCodeWarning);
            Assert.False(vm.SaveCommand.CanExecute(null));
            vm.SaveCommand.Execute(null);
            service.Verify(x => x.SaveRecordForm(It.IsAny<RecordFormDraft>(), It.IsAny<string>()), Times.Never);

            vm.RecordCode = "HS-2026-000001";
            vm.RefreshRecordCodeDuplicateCheck();
            Assert.False(vm.HasDuplicateRecordCode);
            Assert.True(vm.SaveCommand.CanExecute(null));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void NewForm_ShouldDefaultSeverityAndAllowMissingPhoneAndIncidentAddress()
        {
            using var authScope = QuanLyHoSo.Infrastructure.Security.AuthContext.BeginRequestScope(new AppUser
                { Id = 1, UserName = "test-admin", DisplayName = "Test Admin", Role = UserRoles.Admin, IsActive = true });
            var service = NewService();
            service.Setup(x => x.GetCatalogValues("Priority", It.IsAny<bool>()))
                .Returns(new[] { "Ít nghiêm trọng", "Nghiêm trọng" });
            service.Setup(x => x.GetSenderRecords(It.IsAny<RecordFormDraft>())).Returns(new[]
                { new SenderRecordHistory { RecordCode = "original", Status = "Đã giải quyết", IsSameCase = true } });

            using var vm = NewFilledForm(service);
            Assert.Equal("Ít nghiêm trọng", vm.SeverityLevel);

            vm.SenderPhone = string.Empty;
            vm.IncidentAddress = string.Empty;
            vm.RefreshPotentialDuplicateCheck();
            Assert.True(vm.HasPotentialDuplicate);
            service.Verify(x => x.GetSenderRecords(It.Is<RecordFormDraft>(draft =>
                draft.SenderPhone == string.Empty
                && draft.SenderName == "Nguyễn Văn An"
                && draft.AreaName == "Phường Long Xuyên")), Times.Once);
        }

        private static Mock<IApplicationDataService> NewService()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(x => x.GetNextRecordCode()).Returns("HS-2026-000001");
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            return service;
        }

        private static RecordInputViewModel NewFilledForm(Mock<IApplicationDataService> service)
        {
            return new RecordInputViewModel(service.Object)
            {
                SelectedReceivedDate = new DateTime(2026, 9, 13), ReceiveSource = "Trực tiếp", ReceiverName = "Test Admin",
                SenderName = "Nguyễn Văn An", SenderPhone = "0909123456", AreaName = "Phường Long Xuyên",
                ContactAddress = "Phường Long Xuyên", IncidentAddress = "Đường Trần Hưng Đạo",
                CaseType = "Khiếu nại", Content = "Nội dung mới", ContentGroup = "Đất đai", Field = "Hành chính"
            };
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void LoadingDifferentRecords_ShouldShowCorrectReferenceAndClearItForNewForm()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>())).Returns(Array.Empty<string>());
            service.Setup(x => x.GetRecordForm("repeat-a")).Returns(new RecordFormDraft { RecordCode = "repeat-a", OriginalRecordCode = "original-a" });
            service.Setup(x => x.GetRecordForm("repeat-b")).Returns(new RecordFormDraft { RecordCode = "repeat-b", OriginalRecordCode = "original-b" });
            service.Setup(x => x.GetRecordForm("ordinary")).Returns(new RecordFormDraft { RecordCode = "ordinary" });
            using var vm = new RecordInputViewModel(service.Object);
            vm.LoadRecord("repeat-a");
            Assert.True(vm.IsResubmission);
            Assert.Contains("original-a", vm.ResubmissionSummary);
            vm.LoadRecord("repeat-b");
            Assert.Contains("original-b", vm.ResubmissionSummary);
            Assert.DoesNotContain("original-a", vm.ResubmissionSummary);
            vm.LoadRecord("ordinary");
            Assert.False(vm.IsResubmission);
            vm.LoadRecord("repeat-a");
            vm.PrepareNewRecord();
            Assert.False(vm.IsResubmission);
            Assert.Equal("", vm.ResubmissionSummary);
        }
    }
}
