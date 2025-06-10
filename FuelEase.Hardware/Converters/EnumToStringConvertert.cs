using FuelEase.Hardware.Utilities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FuelEase.Hardware.Converters
{
    public class EnumToStringConvertert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum @enum)
            {
                return EnumHelper.GetEnumDisplayName(@enum);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
