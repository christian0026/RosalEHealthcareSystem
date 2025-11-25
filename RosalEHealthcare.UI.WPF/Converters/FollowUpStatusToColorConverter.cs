using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Converters
{
    /// <summary>
    /// Converts follow-up status text to background color for badges
    /// </summary>
    public class FollowUpStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString() ?? "";

            switch (status.ToUpper())
            {
                case "COMPLETED":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));      // Green
                case "OVERDUE":
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));      // Red
                case "1 WEEK":
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));      // Orange
                case "2 WEEKS":
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243));     // Blue
                case "1 MONTH":
                    return new SolidColorBrush(Color.FromRgb(156, 39, 176));     // Purple
                default:
                    if (status.Contains("DAYS"))
                    {
                        var parts = status.Split(' ');
                        if (parts.Length >= 1 && int.TryParse(parts[0], out int days))
                        {
                            if (days <= 3)
                                return new SolidColorBrush(Color.FromRgb(76, 175, 80));   // Green
                            else
                                return new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Orange
                        }
                    }
                    return new SolidColorBrush(Color.FromRgb(117, 117, 117));    // Gray
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}