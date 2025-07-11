using System;
using System.Globalization;
using System.Windows.Data;
using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Converters
{
    public class UnregisteredSaleStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is UnregisteredSaleState state)
            {
                return state switch
                {
                    UnregisteredSaleState.None => "Не зарегистрирован",
                    UnregisteredSaleState.Registered => "Зарегистрирован",
                    _ => string.Empty,
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
