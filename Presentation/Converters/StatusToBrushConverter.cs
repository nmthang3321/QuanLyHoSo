using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyHoSo.Presentation.Converters
{
    public sealed class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString() ?? string.Empty;
            var isForeground = string.Equals(parameter?.ToString(), "Foreground", StringComparison.OrdinalIgnoreCase);

            var color = status switch
            {
                "Đã giải quyết" => isForeground ? "#0D7A2A" : "#DDF8E7",
                "Đang xác minh" => isForeground ? "#075CE8" : "#E7F0FF",
                "Đang phân loại" => isForeground ? "#B45C00" : "#FFF0D6",
                "Phân công" => isForeground ? "#0B5CFF" : "#E7F0FF",
                "Đã phân công" => isForeground ? "#0B5CFF" : "#E7F0FF",
                "Chờ kết quả" => isForeground ? "#5B35C8" : "#EFE9FF",
                "Đang xử lý" => isForeground ? "#B45C00" : "#FFF0D6",
                "Đang chờ bổ sung tài liệu" => isForeground ? "#D42D16" : "#FFE8E3",
                "Chờ bổ sung tài liệu" => isForeground ? "#D42D16" : "#FFE8E3",
                "Chuyển cơ quan khác" => isForeground ? "#1F4AB8" : "#E8EDFF",
                _ => isForeground ? "#5C6B91" : "#EEF3FA"
            };

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
