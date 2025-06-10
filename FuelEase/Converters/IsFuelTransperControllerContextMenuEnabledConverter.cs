using FuelEase.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace FuelEase.Converters
{
    /// <summary>
    /// Этот класс используется для преобразования значения свойства IsFuelTransperControllerContextMenuEnabled в логическое значение.
    /// </summary>
    public class IsFuelTransperControllerContextMenuEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NozzleStatus status)
            {
                return parameter.ToString() switch
                {
                    "StopFilling" => status is NozzleStatus.WaitingRemoved or NozzleStatus.PumpWorking ? true : (object)false,
                    "ContinueFilling" => status is NozzleStatus.WaitingStop or NozzleStatus.PumpStop ? true : (object)false,
                    _ => false
                };
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
