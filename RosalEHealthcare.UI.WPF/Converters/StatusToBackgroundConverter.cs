using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Color.FromRgb(245, 245, 245));

            string status = value.ToString().ToUpper();

            switch (status)
            {
                case "CONFIRMED":
                    return new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Light Blue
                case "PENDING":
                    return new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Light Orange
                case "CANCELLED":
                    return new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light Red
                case "COMPLETED":
                    return new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light Green
                case "RESCHEDULED":
                    return new SolidColorBrush(Color.FromRgb(243, 229, 245)); // Light Purple
                default:
                    return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}