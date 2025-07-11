using KIT.GasStation.HardwareConfigurations.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KIT.GasStation.Hardware.Converters
{
    public class ColumnConnectionStatusForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus connectionStatus)
            {
                switch (connectionStatus)
                {
                    case ConnectionStatus.NotVerified:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#778899"));
                    case ConnectionStatus.BeingVerified:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0C4DE"));
                    case ConnectionStatus.NotConnected:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC143C"));
                    case ConnectionStatus.Connected:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF7F"));
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
