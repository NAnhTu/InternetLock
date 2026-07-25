using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using InternetLock.Models;

namespace InternetLock.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InternetStatus status)
            {
                return status switch
                {
                    InternetStatus.FullyEnabled => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), // Green
                    InternetStatus.FullyDisabled => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), // Red
                    InternetStatus.PartiallyEnabled => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), // Orange/Yellow
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")) // Gray
                };
            }

            if (value is bool isEnabled)
            {
                return isEnabled
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
