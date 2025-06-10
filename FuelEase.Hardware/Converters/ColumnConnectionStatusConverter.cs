using FuelEase.HardwareConfigurations.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FuelEase.Hardware.Converters
{
    public class ColumnConnectionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus connectionStatus)
            {
                switch (connectionStatus)
                {
                    case ConnectionStatus.NotVerified:
                        return "\uf059";
                    case ConnectionStatus.BeingVerified:
                        return "\uf021";
                    case ConnectionStatus.NotConnected:
                        return "\uf057";
                    case ConnectionStatus.Connected:
                        return "\uf058";
                }
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
