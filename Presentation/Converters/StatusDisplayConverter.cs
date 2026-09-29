using System;
using System.Globalization;
using System.Windows.Data;

namespace QuanLyHoSo.Presentation.Converters
{
    public sealed class StatusDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return QuanLyHoSo.Models.RecordStatusDisplay.GetDisplay(value?.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
