using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Converters
{
    public class ReadToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? FontWeights.Normal : FontWeights.SemiBold;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}