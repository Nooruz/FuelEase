using FuelEase.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FuelEase.Converters
{
    public class SelectedNozzleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Nozzle nozzle)
                {
                    if (nozzle.Tank != null)
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(nozzle.Tank.Fuel.ColorHex));
                    }
                }
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7184BA"));
            }
            catch (Exception)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7184BA"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
