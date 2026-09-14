using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class LeaderRecordActionsTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        [Trait("Category", "Regression")]
        public void Leader_ShouldSeeProcessingActionWithoutEditOrDeletePermission()
        {
            using var scope = AuthContext.BeginRequestScope(new AppUser
            {
                Id = 2, UserName = "leader", DisplayName = "Lãnh đạo", Role = UserRoles.Leader, IsActive = true
            });
            var row = new RecordListRowViewModel(new RecentRecord
            {
                RecordCode = "HS-TEST", ProcessorName = "Cán bộ A", Status = "Đang xác minh"
            }, null, null, null, null);
            Assert.True(row.CanClassify);
            Assert.False(row.CanEdit);
            Assert.False(row.CanDelete);
            Assert.False(AuthContext.CanEditRecord(row.ProcessorName));
            Assert.Contains("chỉ xem", row.ProcessingActionToolTip);
        }
    }
}
