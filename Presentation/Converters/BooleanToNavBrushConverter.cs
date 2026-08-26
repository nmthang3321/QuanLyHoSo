using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuanLyHoSo.Presentation.Converters
{
    public sealed class BooleanToNavBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isSelected && isSelected
                ? new SolidColorBrush(Color.FromRgb(11, 92, 255))
                : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
