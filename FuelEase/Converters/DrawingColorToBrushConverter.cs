using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;  // Для удобства

namespace FuelEase.Converters
{
    public class DrawingColorToBrushConverter : IValueConverter
    {
        // Преобразование System.Drawing.Color в System.Windows.Media.Brush
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DrawingColor drawingColor)
            {
                // Преобразование Drawing.Color в Media.Color
                Color mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                return new SolidColorBrush(mediaColor);  // Преобразование Media.Color в SolidColorBrush
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
