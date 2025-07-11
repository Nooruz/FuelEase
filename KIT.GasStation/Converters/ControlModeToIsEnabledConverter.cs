using KIT.GasStation.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class ControlModeToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FuelTransperControllerControlMode controlMode)
            {
                if (controlMode != FuelTransperControllerControlMode.None)
                {
                    return true;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
