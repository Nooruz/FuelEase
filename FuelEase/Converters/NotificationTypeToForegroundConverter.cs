using FuelEase.State.Notifications;
using System;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;

namespace FuelEase.Converters
{
    public class NotificationTypeToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Information => Brushes.Blue,// или другой цвет для информационных сообщений
                    NotificationType.Warning => Brushes.Orange,// для предупреждений
                    NotificationType.Error => Brushes.Red,// для ошибок
                    _ => Brushes.Transparent,
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
