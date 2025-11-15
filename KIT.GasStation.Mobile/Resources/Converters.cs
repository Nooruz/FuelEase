using System.Globalization;
using DrawingColor = System.Drawing.Color;

namespace KIT.GasStation.Mobile.Resources
{
    public class DrawingColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DrawingColor drawingColor)
            {
                // Преобразование System.Drawing.Color → Microsoft.Maui.Graphics.Color
                var mauiColor = Microsoft.Maui.Graphics.Color.FromRgba(
                    drawingColor.R,
                    drawingColor.G,
                    drawingColor.B,
                    drawingColor.A
                );

                return new SolidColorBrush(mauiColor);
            }

            return Colors.Transparent; // безопасный fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
