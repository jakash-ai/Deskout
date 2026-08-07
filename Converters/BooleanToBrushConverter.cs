using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Deskout.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = value is bool b && b;
            string param = parameter as string ?? string.Empty;

            if (param == "RedGreen")
            {
                // Warning (red) if true (e.g. process is running), Green if false (not running)
                return isTrue 
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 57, 53))  // Red
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));  // Green
            }

            return isTrue ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
