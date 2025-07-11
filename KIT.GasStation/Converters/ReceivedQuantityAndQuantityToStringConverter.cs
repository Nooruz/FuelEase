using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class ReceivedQuantityAndQuantityToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return "0 / 0";
            }

            double quantity = 0;
            double receivedQuantity = 0;

            if (double.TryParse(values[1].ToString(), out double q))
            {
                quantity = q;
            }

            if (double.TryParse(values[0].ToString(), out double r))
            {
                receivedQuantity = r;
            }

            return $"{receivedQuantity:N2} / {quantity:N2}";

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
