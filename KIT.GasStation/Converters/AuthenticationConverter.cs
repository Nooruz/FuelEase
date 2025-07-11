using KIT.GasStation.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class AuthenticationConverter : IValueConverter
    {
        // Преобразует IntegratedSecurity (bool) в AuthenticationType
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool integratedSecurity)
            {
                return integratedSecurity ? AuthenticationType.Windows : AuthenticationType.SQLServer;
            }
            return AuthenticationType.Windows;
        }

        // Преобразует AuthenticationType обратно в IntegratedSecurity (bool)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AuthenticationType authType)
            {
                return authType == AuthenticationType.Windows;
            }
            return true;
        }
    }
}
