using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            bool invert = (parameter as string) == "Invert";
            bool isNullOrEmpty = string.IsNullOrWhiteSpace(s);

            // Default: Visible when string is null/empty, Collapsed when it has value
            bool visible = isNullOrEmpty;

            // If "Invert" parameter, reverse the logic
            if (invert)
                visible = !visible;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}