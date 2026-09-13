using System;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class RequiredPasswordChangeViewModelTests
    {
        [Theory]
        [InlineData("", "abcdef", "abcdef")]
        [InlineData("old", "abcde", "abcde")]
        [InlineData("old", "abcdef", "different")]
        [InlineData("same-password", "same-password", "same-password")]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void ChangePassword_WhenValidationFails_ShouldNotCallService(string current, string next, string confirmation)
        {
            var service = new Mock<IApplicationDataService>(MockBehavior.Strict);
            var viewModel = Create(service.Object);
            viewModel.CurrentPassword = current;
            viewModel.NewPassword = next;
            viewModel.ConfirmPassword = confirmation;

            viewModel.ChangePasswordCommand.Execute(null);

            Assert.True(viewModel.HasError);
            service.VerifyNoOtherCalls();
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Security)]
        public void ChangePassword_WhenCurrentPasswordIsWrong_ShouldShowErrorAndNotComplete()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(item => item.ChangeCurrentUserPassword("old-password", "new-password")).Returns(false);
            var completed = 0;
            var viewModel = Create(service.Object, _ => completed++);
            viewModel.CurrentPassword = "old-password";
            viewModel.NewPassword = "new-password";
            viewModel.ConfirmPassword = "new-password";

            viewModel.ChangePasswordCommand.Execute(null);

            Assert.True(viewModel.HasError);
            Assert.Equal(0, completed);
            service.Verify(item => item.ChangeCurrentUserPassword("old-password", "new-password"), Times.Once);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Critical)]
        public void ChangePassword_WhenServiceSucceeds_ShouldClearForcedChangeAndCompleteOnce()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(item => item.ChangeCurrentUserPassword("old-password", "new-password")).Returns(true);
            AppUser completedUser = null;
            var user = User();
            var viewModel = new RequiredPasswordChangeViewModel(service.Object, user, result => completedUser = result, () => { });
            viewModel.CurrentPassword = "old-password";
            viewModel.NewPassword = "new-password";
            viewModel.ConfirmPassword = "new-password";

            viewModel.ChangePasswordCommand.Execute(null);

            Assert.False(viewModel.HasError);
            Assert.False(user.MustChangePassword);
            Assert.Same(user, completedUser);
            service.Verify(item => item.ChangeCurrentUserPassword("old-password", "new-password"), Times.Once);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void CancelCommand_ShouldInvokeCallbackExactlyOnce()
        {
            var service = new Mock<IApplicationDataService>();
            var cancellations = 0;
            var viewModel = new RequiredPasswordChangeViewModel(service.Object, User(), _ => { }, () => cancellations++);

            viewModel.CancelCommand.Execute(null);

            Assert.Equal(1, cancellations);
        }

        private static RequiredPasswordChangeViewModel Create(IApplicationDataService service, Action<AppUser> completed = null) =>
            new RequiredPasswordChangeViewModel(service, User(), completed ?? (_ => { }), () => { });

        private static AppUser User() => new AppUser
        {
            UserName = "officer",
            DisplayName = "Cán bộ thử nghiệm",
            Role = UserRoles.Officer,
            MustChangePassword = true,
            IsActive = true
        };
    }
}
