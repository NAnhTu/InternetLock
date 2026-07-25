using System;
using System.Globalization;
using System.Windows.Data;

namespace InternetLock.Converters
{
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
            {
                return boolVal ? "Đang bật" : "Đã tắt";
            }
            return "Chưa rõ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
