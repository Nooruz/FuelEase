using FuelEase.State.Notifications;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FuelEase.Converters
{
    public class NotificationTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Information => Application.Current.Resources["faCircleInfo"] as string,
                    NotificationType.Warning => Application.Current.Resources["faTriangleExclamation"] as string,
                    NotificationType.Error => Application.Current.Resources["faBrakeWarning"] as string,
                    _ => string.Empty,
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
