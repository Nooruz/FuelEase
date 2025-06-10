using FuelEase.Domain.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FuelEase.Converters
{
    public class NozzleStatusToBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusNoneColor"]);
            }

            if (values.Any(v => v == DependencyProperty.UnsetValue))
            {
                return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusNoneColor"]);
            }

            NozzleStatus status = NozzleStatus.Unknown;

            if (values[0] is NozzleStatus nozzleStatus)
            {
                status = nozzleStatus;
            }

            NozzleControlMode controlMode = (NozzleControlMode)values[1];

            //if (command == FuelNozzleCommand.Block)
            //{
            //    return new SolidColorBrush((Color)Application.Current.Resources["FuelNozzleBlockColor"]);
            //}

            if (controlMode == NozzleControlMode.Keyboard)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["NozzleControlModeProgramColor"]);
            }

            switch (status)
            {
                case NozzleStatus.Unknown:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusNoneColor"]);
                case NozzleStatus.Ready:
                    return new SolidColorBrush((Color)Application.Current.Resources["WhiteColor"]);
                case NozzleStatus.PumpWorking:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusPumpWorkingColor"]);
                case NozzleStatus.WaitingStop:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusWaitingStopColor"]);
                case NozzleStatus.PumpStop:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusPumpStopColor"]);
                case NozzleStatus.WaitingRemoved:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleStatusNozzleWaitingRemovedColor"]);
                case NozzleStatus.Blocking:
                    return new SolidColorBrush((Color)Application.Current.Resources["NozzleBlockColor"]);
            }
            return Colors.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
