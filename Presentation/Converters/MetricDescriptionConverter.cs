using System;
using System.Globalization;
using System.Windows.Data;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Presentation.Converters
{
    public sealed class MetricDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StaffTrackingMetric staff)
            {
                var description = staff.FilterKey switch
                {
                    "Total" => AuthContext.IsOfficer
                        ? "Số hồ sơ được giao cho bạn, gồm cả hồ sơ đang xử lý và đã giải quyết."
                        : "Số cán bộ có hồ sơ được giao trong kỳ đang chọn.",
                    "Overloaded" => AuthContext.IsOfficer
                        ? "Số hồ sơ được giao cho bạn chưa giải quyết, bao gồm hồ sơ đang chờ tài liệu hoặc kết quả."
                        : "Số cán bộ có từ 10 hồ sơ chưa giải quyết trở lên, bao gồm hồ sơ đang chờ tài liệu hoặc kết quả.",
                    "DueSoon" => AuthContext.IsOfficer
                        ? "Số hồ sơ của bạn chưa giải quyết, có ngày dự kiến trả kết quả từ hôm nay đến hết 7 ngày tới."
                        : "Số cán bộ có ít nhất một hồ sơ chưa giải quyết với ngày dự kiến trả kết quả từ hôm nay đến hết 7 ngày tới.",
                    "NeedsAttention" => AuthContext.IsOfficer
                        ? "Số hồ sơ của bạn chưa giải quyết và đã qua ngày dự kiến trả kết quả."
                        : "Số cán bộ có hồ sơ chưa giải quyết đã quá hạn hoặc tỷ lệ giải quyết đúng hạn dưới 80% (KPI cần cải thiện).",
                    _ => string.Empty
                };
                return description + " Thống kê theo ngày tiếp nhận trong kỳ đang chọn; không tính hồ sơ gửi lại và hồ sơ trong thùng rác. Bấm card để lọc danh sách cán bộ.";
            }

            if (value is DashboardMetric metric)
            {
                if (string.Equals(parameter as string, "Processing", StringComparison.Ordinal))
                {
                    var description = metric.FilterKey switch
                    {
                        "All" => "Tất cả hồ sơ chưa giải quyết trong phạm vi bạn được xem.",
                        "NeedClassify" => "Hồ sơ ở trạng thái Mới tiếp nhận hoặc Đang phân loại.",
                        "Processing" => "Hồ sơ ở trạng thái Đã phân công hoặc Đang xác minh.",
                        "Waiting" => "Hồ sơ ở trạng thái Chờ kết quả hoặc Đang chờ bổ sung tài liệu.",
                        "DueSoon" => "Hồ sơ chưa giải quyết có ngày dự kiến trả kết quả từ hôm nay đến hết 7 ngày tới.",
                        "Overdue" => "Hồ sơ chưa giải quyết đã qua ngày dự kiến trả kết quả.",
                        "HighPriority" => "Hồ sơ chưa giải quyết có mức độ Nghiêm trọng, Rất nghiêm trọng hoặc Đặc biệt nghiêm trọng.",
                        _ => string.Empty
                    };
                    return description + " Không tính hồ sơ trong thùng rác. Số trên card không phụ thuộc bộ lọc danh sách bên dưới. Bấm card để lọc hồ sơ.";
                }

                var dashboardDescription = metric.Title switch
                {
                    "TỔNG HỒ SƠ" => "Tổng số hồ sơ tiếp nhận, gồm mọi trạng thái và cả hồ sơ gửi lại.",
                    "ĐANG XỬ LÝ" => "Số hồ sơ ở trạng thái Đang phân loại, Đã phân công, Đang xác minh hoặc Đang xử lý.",
                    "ĐÃ GIẢI QUYẾT" => "Số hồ sơ ở trạng thái Đã giải quyết; không tính hồ sơ gửi lại vào số đã giải quyết.",
                    "CHỜ KẾT QUẢ" => "Số hồ sơ ở trạng thái Chờ kết quả hoặc Đang chờ bổ sung tài liệu.",
                    _ => string.Empty
                };
                return dashboardDescription + " Thống kê theo ngày tiếp nhận trong kỳ đang chọn và phạm vi bạn được xem; không tính hồ sơ trong thùng rác. Mức tăng/giảm so với kỳ trước có cùng độ dài.";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
