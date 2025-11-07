using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class NameInitialsConverter : IValueConverter
    {
        // Produces initials from full name. Eg: "Maria Santos" -> "MS"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string s) || string.IsNullOrWhiteSpace(s)) return "NA";
            var parts = s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "NA";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return string.Concat(parts.Select(p => p[0])).ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
