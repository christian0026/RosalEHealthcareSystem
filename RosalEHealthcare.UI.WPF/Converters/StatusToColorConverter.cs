using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Colors.Gray);

            string status = value.ToString().ToUpper();

            switch (status)
            {
                case "CONFIRMED":
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                case "PENDING":
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                case "CANCELLED":
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                case "COMPLETED":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                case "RESCHEDULED":
                    return new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}