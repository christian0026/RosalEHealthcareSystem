using System;
using System.Globalization;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class BoolToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0; // Yes = 1, No = 0
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index == 1; // 1 = Yes (true), 0 = No (false)
            }
            return false;
        }
    }
}