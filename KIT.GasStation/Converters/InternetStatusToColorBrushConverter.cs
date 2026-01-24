using KIT.GasStation.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace KIT.GasStation.Converters
{
    public class InternetStatusToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InternetStatus status)
            {
                return status switch
                {
                    InternetStatus.Checking => new SolidColorBrush((Color)Application.Current.Resources["InternetCheckingColor"]),
                    InternetStatus.Connected => new SolidColorBrush((Color)Application.Current.Resources["InternetConnectedColor"]),
                    InternetStatus.Disconnected => new SolidColorBrush((Color)Application.Current.Resources["InternetDisconnectedColor"]),
                    _ => new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                };
            }
            return new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
