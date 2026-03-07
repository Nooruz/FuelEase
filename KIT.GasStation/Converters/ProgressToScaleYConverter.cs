using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class ProgressToScaleYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return new CornerRadius(0);

            double value = System.Convert.ToDouble(values[0]);
            double min = System.Convert.ToDouble(values[1]);
            double max = System.Convert.ToDouble(values[2]);

            if (max <= min)
                return new CornerRadius(0);

            double percent = (value - min) / (max - min);

            // считаем "почти максимум" = 93%+
            if (percent >= 0.93)
            {
                // скругляем верх тоже
                return new CornerRadius(5);
            }

            // иначе только нижние углы
            return new CornerRadius(5, 0, 0, 5);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
