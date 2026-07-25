using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InternetLock.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value != null && !string.IsNullOrWhiteSpace(value.ToString());
            if (parameter?.ToString() == "Inverse")
            {
                isVisible = !isVisible;
            }
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
