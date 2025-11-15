using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    /// <summary>
    /// Returns Visible when the provided string is null or empty; otherwise Collapsed.
    /// Parameter = "Invert" will invert behavior (Visible when NOT empty).
    /// </summary>
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            bool invert = (parameter as string) == "Invert";
            bool isNullOrEmpty = string.IsNullOrWhiteSpace(s);

            bool visible = isNullOrEmpty;
            if (invert) visible = !visible;

            return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
