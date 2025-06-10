using System;
using System.Globalization;
using System.Windows.Data;

namespace FuelEase.Converters
{
    public class EnumToStringConvertert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum @enum)
            {
                return Helpers.EnumHelper.GetEnumDisplayName(@enum);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
