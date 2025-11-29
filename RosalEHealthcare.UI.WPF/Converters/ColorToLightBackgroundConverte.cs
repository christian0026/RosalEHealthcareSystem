using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class ColorToLightBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorStr);
                    // Create a lighter version (20% opacity)
                    var lightColor = Color.FromArgb(51, color.R, color.G, color.B); // 51 = 20% of 255
                    return new SolidColorBrush(lightColor);
                }
                catch
                {
                    return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                }
            }
            return new SolidColorBrush(Color.FromRgb(245, 245, 245));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}