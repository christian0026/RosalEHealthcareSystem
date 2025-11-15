using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class NameInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string s) || string.IsNullOrWhiteSpace(s)) return "?";

            var parts = s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return "?";
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();

            // First letter of first name + first letter of last name
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}