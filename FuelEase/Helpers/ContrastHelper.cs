using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuelEase.Helpers
{
    public static class ContrastHelper
    {
        public static readonly DependencyProperty AutoContrastForegroundProperty =
            DependencyProperty.RegisterAttached(
                "AutoContrastForeground",
                typeof(bool),
                typeof(ContrastHelper),
                new PropertyMetadata(false, OnAutoContrastChanged));

        public static bool GetAutoContrastForeground(DependencyObject obj) =>
            (bool)obj.GetValue(AutoContrastForegroundProperty);

        public static void SetAutoContrastForeground(DependencyObject obj, bool value) =>
            obj.SetValue(AutoContrastForegroundProperty, value);

        private static void OnAutoContrastChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                // При загрузке обновляем цвет
                element.Loaded += (s, args) => UpdateForeground(element);

                // Если у элемента изменяется его собственный фон — обновляем.
                var dp = GetDependencyProperty(element, "Background");
                if (dp != null)
                {
                    var descriptor = DependencyPropertyDescriptor.FromProperty(dp, element.GetType());
                    if (descriptor != null)
                    {
                        descriptor.AddValueChanged(element, (s, args) => UpdateForeground(element));
                    }
                }
            }
        }

        /// <summary>
        /// Пытается найти DependencyProperty для имени (например, "Background")
        /// </summary>
        private static DependencyProperty GetDependencyProperty(FrameworkElement element, string propertyName)
        {
            var field = element.GetType().GetField(propertyName + "Property",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (field != null && field.GetValue(null) is DependencyProperty dp)
                return dp;
            return null;
        }

        /// <summary>
        /// Ищет эффективный фон для элемента.
        /// Если у самого элемента фон не задан (null), пробует найти предка с заданным фоном.
        /// </summary>
        private static SolidColorBrush GetEffectiveBackground(FrameworkElement element)
        {
            // Попробуем получить фон элемента
            var brush = GetPropertyValue<Brush>(element, "Background") as SolidColorBrush;
            if (brush != null)
                return brush;

            // Если фон не задан, идём вверх по визуальному дереву
            DependencyObject parent = element;
            while (parent != null)
            {
                // Для некоторых элементов (например, TextBlock) фон может не задан,
                // но если предок — Border, Panel, Control и т.п., то там он обычно есть.
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe)
                {
                    brush = GetPropertyValue<Brush>(fe, "Background") as SolidColorBrush;
                    if (brush != null)
                        return brush;
                }
            }
            // Если нигде не найдено, возвращаем белый по умолчанию (или другой подходящий цвет)
            return Brushes.White as SolidColorBrush;
        }

        private static void UpdateForeground(FrameworkElement element)
        {
            // Получаем "эффективный" фон — то есть, фон элемента или ближайшего предка.
            SolidColorBrush effectiveBackground = GetEffectiveBackground(element);

            if (effectiveBackground != null)
            {
                var color = effectiveBackground.Color;
                // Вычисляем яркость по формуле: (299*R + 587*G + 114*B) / 1000
                double brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000.0;
                // Если фон светлый (больше или равен порогу), текст делаем чёрным, иначе – белым.
                // Здесь порог можно настроить, например, 128 или 150, в зависимости от предпочтений.
                var newForeground = brightness >= 128 ? Brushes.Black : Brushes.White;
                SetPropertyValue(element, "Foreground", newForeground);
            }
        }

        /// <summary>
        /// Получает значение свойства у объекта через reflection.
        /// </summary>
        private static T GetPropertyValue<T>(object obj, string propertyName) where T : class
        {
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(obj) as T;
        }

        /// <summary>
        /// Устанавливает значение свойства у объекта через reflection.
        /// </summary>
        private static void SetPropertyValue(object obj, string propertyName, object value)
        {
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }
    }
}
