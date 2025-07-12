using KIT.GasStation.Domain.Models;
using KIT.GasStation.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    /// <summary>
    /// Конвертер для определения содержимого кнопки в зависимости от параметра
    /// </summary>
    public class BarButtonItemContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter.ToString() switch
            {
                "Block" => value is NozzleStatus status ? status == NozzleStatus.Blocking ? "Разблокировать ТРК" : (object)"Блокировать ТРК" : "Блокировать ТРК",
                "ControlMode" => value is FuelTransperControllerControlMode mode ? mode == FuelTransperControllerControlMode.Keyboard ? "Откл. автоном. режим" : (object)"Вкл. автоном. режим" : "Вкл. автоном. режим",
                _ => "",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
