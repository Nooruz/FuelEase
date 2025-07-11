using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KIT.GasStation.Converters
{
    public class NozzleMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is ListViewItem item && values[1] is int count)
            {
                var listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
                if (listView != null)
                {
                    int index = listView.ItemContainerGenerator.IndexFromContainer(item);
                    if (index == 0)
                    {
                        return new Thickness(0, 0, 2, 0); // Первый элемент
                    }
                    else if (index == count - 1)
                    {
                        return new Thickness(2, 0, 0, 0); // Последний элемент
                    }
                    else
                    {
                        return new Thickness(2, 0, 2, 0); // Все остальные элементы
                    }
                }
            }
            return new Thickness(2, 0, 2, 0); // Значение по умолчанию
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
