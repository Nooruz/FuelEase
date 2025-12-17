using KIT.GasStation.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    /// <summary>
    /// Этот класс используется для преобразования значения свойства IsFuelTransperControllerContextMenuEnabled в логическое значение.
    /// </summary>
    public class NozzleStatusToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NozzleStatus status)
            {
                return parameter.ToString() switch
                {
                    "StopFueling" => status is NozzleStatus.WaitingRemoved or NozzleStatus.PumpWorking,
                    "ResumeFueling" => status is NozzleStatus.WaitingStop or NozzleStatus.PumpStop,
                    "Pumping" => status is NozzleStatus.Ready,
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
