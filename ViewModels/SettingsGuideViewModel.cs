using System;
using System.Collections.Generic;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Security;

namespace QuanLyHoSo.ViewModels
{
    public sealed class SettingsGuideViewModel : ViewModelBase
    {
        public SettingsGuideViewModel(Action back)
        {
            BackCommand = new RelayCommand(back ?? throw new ArgumentNullException(nameof(back)));
            UserDisplayName = AuthContext.CurrentDisplayName;
            RoleText = AuthContext.CurrentUser?.RoleText ?? "Người dùng";

            if (AuthContext.IsAdmin)
            {
                IntroText = "Bạn có toàn quyền quản lý dữ liệu, tài khoản và cấu hình hệ thống.";
                Steps = BuildAdminSteps();
                Topics = BuildAdminTopics();
                Tips = new[]
                {
                    "Sao lưu trước khi thay đổi nhiều dữ liệu.",
                    "Cấp đúng quyền và xem Nhật ký khi cần đối chiếu."
                };
                return;
            }

            if (AuthContext.IsLeader)
            {
                IntroText = "Bạn theo dõi hồ sơ, tiến độ và hiệu suất của cán bộ.";
                Steps = BuildLeaderSteps();
                Topics = BuildLeaderTopics();
                Tips = new[]
                {
                    "Chọn đúng kỳ trước khi xem số liệu hoặc đặt KPI.",
                    "Kiểm tra deadline trước khi gửi nhắc nhở."
                };
                return;
            }

            IntroText = "Bạn xử lý hồ sơ được giao và theo dõi tiến độ của mình.";
            Steps = BuildOfficerSteps();
            Topics = BuildOfficerTopics();
            Tips = new[]
            {
                "Ưu tiên hồ sơ sắp đến hạn hoặc quá hạn.",
                "Xem Theo dõi cán bộ để đọc thông báo mới."
            };
        }

        public string UserDisplayName { get; }
        public string RoleText { get; }
        public string IntroText { get; }
        public IReadOnlyList<SettingsGuideStep> Steps { get; }
        public IReadOnlyList<SettingsGuideTopic> Topics { get; }
        public IReadOnlyList<string> Tips { get; }
        public ICommand BackCommand { get; }

        private static IReadOnlyList<SettingsGuideStep> BuildAdminSteps()
        {
            return new[]
            {
                Step("01", "Tổng quan", "Xem nhanh tình hình hồ sơ.", "\uE80F"),
                Step("02", "Nhập dữ liệu", "Tạo và nhập thông tin hồ sơ mới.", "\uE8A5"),
                Step("03", "Danh sách hồ sơ", "Tìm, xem và quản lý hồ sơ.", "\uE8FD"),
                Step("04", "Phân loại & xử lý", "Phân công và cập nhật tiến độ.", "\uE72C"),
                Step("05", "Theo dõi cán bộ", "Xem deadline, hiệu suất và KPI.", "\uE716"),
                Step("06", "Cài đặt", "Quản lý hệ thống và sao lưu.", "\uE713")
            };
        }

        private static IReadOnlyList<SettingsGuideStep> BuildLeaderSteps()
        {
            return new[]
            {
                Step("01", "Tổng quan", "Xem tình hình chung của hồ sơ.", "\uE80F"),
                Step("02", "Danh sách hồ sơ", "Lọc và xem hồ sơ cần theo dõi.", "\uE8FD"),
                Step("03", "Phân loại & xử lý", "Theo dõi hàng đợi và tiến độ.", "\uE72C"),
                Step("04", "Theo dõi cán bộ", "Xem hiệu suất, gửi nhắc nhở và đặt KPI.", "\uE716"),
                Step("05", "Cài đặt", "Đổi mật khẩu và xem nhật ký.", "\uE713")
            };
        }

        private static IReadOnlyList<SettingsGuideStep> BuildOfficerSteps()
        {
            return new[]
            {
                Step("01", "Danh sách hồ sơ", "Tìm và xem hồ sơ được giao.", "\uE8FD"),
                Step("02", "Phân loại & xử lý", "Cập nhật tiến độ và kết quả.", "\uE72C"),
                Step("03", "Theo dõi cán bộ", "Xem deadline, KPI và thông báo.", "\uE716"),
                Step("04", "Cài đặt", "Đổi mật khẩu và xem nhật ký.", "\uE713")
            };
        }

        private static IReadOnlyList<SettingsGuideTopic> BuildAdminTopics()
        {
            return new[]
            {
                RecordFilterTopic(includeAllStaff: true),
                StaffTrackingFilterTopic(),
                PerformanceTopic(),
                LeadershipKpiTopic(),
                Topic(
                    "QUẢN TRỊ AN TOÀN",
                    "Danh mục, người dùng và sao lưu",
                    "Các chức năng này chỉ dành cho Admin.",
                    "Danh mục: sửa các lựa chọn dùng trong form và bộ lọc.",
                    "Người dùng: tạo tài khoản và chọn đúng vai trò.",
                    "Sao lưu dữ liệu trước khi thay đổi lớn.")
            };
        }

        private static IReadOnlyList<SettingsGuideTopic> BuildLeaderTopics()
        {
            return new[]
            {
                RecordFilterTopic(includeAllStaff: true),
                StaffTrackingFilterTopic(),
                PerformanceTopic(),
                LeadershipKpiTopic()
            };
        }

        private static IReadOnlyList<SettingsGuideTopic> BuildOfficerTopics()
        {
            return new[]
            {
                RecordFilterTopic(includeAllStaff: false),
                StaffTrackingFilterTopic(),
                PerformanceTopic(),
                Topic(
                    "XỬ LÝ HỒ SƠ",
                    "Cập nhật đúng hồ sơ của bạn",
                    "Bạn chỉ được sửa hồ sơ phân công cho mình.",
                    "Tìm hồ sơ, chọn Phân loại & xử lý, cập nhật trạng thái và kết quả rồi lưu.",
                    "Nếu phân công sai hoặc cần lùi quy trình, liên hệ Admin.")
            };
        }

        private static SettingsGuideTopic RecordFilterTopic(bool includeAllStaff)
        {
            var staffScope = includeAllStaff
                ? "Chọn Người xử lý để xem một cán bộ; chọn Tất cả để xem toàn bộ."
                : "Bạn chỉ thấy hồ sơ được phân công cho mình.";

            return Topic(
                "BỘ LỌC HỒ SƠ",
                "Tìm đúng hồ sơ cần xem",
                "Các điều kiện lọc được áp dụng cùng lúc.",
                "Chọn khoảng ngày, trạng thái hoặc danh mục; nhập từ khóa nếu cần.",
                staffScope,
                "Bấm Xem dữ liệu để lọc; bấm Đặt lại để xem từ đầu.") ;
        }

        private static SettingsGuideTopic StaffTrackingFilterTopic()
        {
            return Topic(
                "BỘ LỌC THEO DÕI",
                "Chọn đúng kỳ và dùng card lọc nhanh",
                "Chọn Tuần, Tháng, Năm hoặc khoảng ngày Khác rồi bấm Áp dụng.",
                "Số liệu trong kỳ được tính theo ngày tiếp nhận hồ sơ.",
                "Bấm card Đang xử lý, Sắp trễ hạn hoặc Quá hạn để lọc nhanh; bấm card tổng để xem lại tất cả.") ;
        }

        private static SettingsGuideTopic PerformanceTopic()
        {
            return Topic(
                "CÁCH TÍNH HIỆU SUẤT",
                "Hiểu các số liệu trước khi đánh giá",
                "Đang xử lý là hồ sơ chưa giải quyết; Hoàn thành là hồ sơ đã giải quyết.",
                "Sắp trễ hạn: còn tối đa 7 ngày. Quá hạn: đã qua hạn nhưng chưa hoàn thành.",
                "Đúng hạn = hồ sơ hoàn thành đúng hạn / hồ sơ có theo dõi hạn × 100%.",
                "Từ 90%: Tốt; từ 80%: Khá; dưới 80%: Cần cải thiện. Rê chuột vào biểu đồ để xem số chính xác.") ;
        }

        private static SettingsGuideTopic LeadershipKpiTopic()
        {
            return Topic(
                "ĐẶT KPI CHO CÁN BỘ",
                "Thiết lập chỉ tiêu số hồ sơ hoàn thành",
                "Chỉ Admin và Lãnh đạo được đặt KPI.",
                "Chọn cán bộ, mở Đặt KPI, nhập chỉ tiêu lớn hơn 0 rồi bấm Lưu KPI.",
                "Tiến độ KPI = số hồ sơ hoàn thành / chỉ tiêu × 100%. Ví dụ 18/30 = 60%.",
                "KPI mục tiêu khác cột KPI trong bảng; cột trong bảng đang thể hiện tỷ lệ đúng hạn.") ;
        }

        private static SettingsGuideTopic Topic(string eyebrow, string title, string description, params string[] items)
        {
            return new SettingsGuideTopic
            {
                Eyebrow = eyebrow,
                Title = title,
                Description = description,
                Items = items
            };
        }

        private static SettingsGuideStep Step(string number, string title, string description, string iconGlyph)
        {
            return new SettingsGuideStep
            {
                Number = number,
                Title = title,
                Description = description,
                IconGlyph = iconGlyph
            };
        }
    }

    public sealed class SettingsGuideStep
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconGlyph { get; set; }
    }

    public sealed class SettingsGuideTopic
    {
        public string Eyebrow { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<string> Items { get; set; }
    }
}
