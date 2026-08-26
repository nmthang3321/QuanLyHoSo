using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.Presentation.Converters
{
    public sealed class StatusDonutSegmentConverter : IValueConverter
    {
        private const double Center = 95;
        private const double Radius = 76;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StatusStat stat || stat.SweepAngle <= 0)
            {
                return Geometry.Empty;
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                if (stat.SweepAngle >= 359.99)
                {
                    DrawArc(context, stat.StartAngle, 180, false);
                    DrawArc(context, stat.StartAngle + 180, 180, true);
                }
                else
                {
                    DrawArc(context, stat.StartAngle, stat.SweepAngle, false);
                }
            }

            geometry.Freeze();
            return geometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static void DrawArc(StreamGeometryContext context, double startAngle, double sweepAngle, bool isConnected)
        {
            var start = PointAt(startAngle);
            var end = PointAt(startAngle + sweepAngle);

            context.BeginFigure(start, false, false);
            context.ArcTo(
                end,
                new Size(Radius, Radius),
                0,
                Math.Abs(sweepAngle) > 180,
                SweepDirection.Clockwise,
                true,
                false);
        }

        private static Point PointAt(double angle)
        {
            var radians = angle * Math.PI / 180;
            return new Point(
                Center + Radius * Math.Cos(radians),
                Center + Radius * Math.Sin(radians));
        }
    }
}
