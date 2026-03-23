using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SyncDB.Core
{
    /// <summary>
    /// Đảo ngược bool: true → false, false → true
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

    /// <summary>
    /// Watch status → Background brush: true = green, false = gray
    /// </summary>
    public class WatchStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isWatching = value is bool b && b;
            return isWatching
                ? (Brush)Application.Current.FindResource("AccentGreenBrush")
                : (Brush)new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Watch status → Text: true = "WATCHING", false = "IDLE"
    /// </summary>
    public class WatchStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? "👁 WATCHING" : "IDLE";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
