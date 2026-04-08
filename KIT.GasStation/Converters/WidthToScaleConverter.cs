using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class WidthToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double currentWidth || currentWidth <= 0)
            {
                return 1d;
            }

            var baseWidth = 320d;
            if (parameter is string parameterText
                && double.TryParse(parameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedBaseWidth)
                && parsedBaseWidth > 0)
            {
                baseWidth = parsedBaseWidth;
            }

            var scale = currentWidth / baseWidth;
            var snappedScale = Math.Round(scale * 8d, MidpointRounding.AwayFromZero) / 8d;

            if (snappedScale < 0.5d)
            {
                return 0.5d;
            }

            return snappedScale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
