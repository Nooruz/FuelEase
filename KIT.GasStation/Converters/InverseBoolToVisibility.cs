using System;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class InverseBoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
            return System.Windows.Visibility.Hidden;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility != System.Windows.Visibility.Visible;
            }
            return false;
        }
    }
}
