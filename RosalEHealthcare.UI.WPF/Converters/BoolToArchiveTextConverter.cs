using System;
using System.Globalization;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class BoolToArchiveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Archive Medicine" : "Restore Medicine";
            }
            return "Archive Medicine";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}