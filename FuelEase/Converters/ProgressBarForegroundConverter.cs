using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FuelEase.Converters
{
    public class ProgressBarForegroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["TankForegroundColor"]); // Default color if not enough values are provided
            }

            double currentFuelQuantity = (double)values[0];
            double minimumSize = (double)values[1];

            // Check if the difference is less than or equal to MinimumSize
            if (currentFuelQuantity <= minimumSize)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["TankMinimumForegroundColor"]); // Set to red if difference is less than or equal to MinimumSize
            }
            else
            {
                return new SolidColorBrush((Color)Application.Current.Resources["TankForegroundColor"]); // Set to blue if difference is greater than MinimumSize
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
