using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KIT.GasStation.Converters
{
    internal class BackgroundToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not SolidColorBrush brush)
                return Brushes.Black;

            var color = brush.Color;

            // Формула яркости (перцептивная)
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);

            return brightness < 128 ? Brushes.White : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
    }
}
