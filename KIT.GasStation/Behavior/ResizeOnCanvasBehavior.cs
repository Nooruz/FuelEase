using KIT.GasStation.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace KIT.GasStation.Behavior
{
    public enum ResizeDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public static class ResizeOnCanvasBehavior
    {
        #region Attached Properties

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ResizeOnCanvasBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsEnabledProperty, value);

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.RegisterAttached(
                "Direction",
                typeof(ResizeDirection),
                typeof(ResizeOnCanvasBehavior),
                new PropertyMetadata(ResizeDirection.Right));

        public static ResizeDirection GetDirection(DependencyObject obj) =>
            (ResizeDirection)obj.GetValue(DirectionProperty);

        public static void SetDirection(DependencyObject obj, ResizeDirection value) =>
            obj.SetValue(DirectionProperty, value);

        // Прямоугольник карточки в момент начала ресайза.
        // Используется в IntersectsNew чтобы разрешить движение,
        // если элементы уже перекрывались до начала операции.
        private static readonly DependencyProperty DragInitialRectProperty =
            DependencyProperty.RegisterAttached(
                "DragInitialRect",
                typeof(Rect),
                typeof(ResizeOnCanvasBehavior),
                new PropertyMetadata(Rect.Empty));

        #endregion

        #region Subscription

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Thumb thumb)
                return;

            if ((bool)e.NewValue)
            {
                thumb.DragStarted += Thumb_DragStarted;
                thumb.DragDelta   += Thumb_DragDelta;
            }
            else
            {
                thumb.DragStarted -= Thumb_DragStarted;
                thumb.DragDelta   -= Thumb_DragDelta;
            }
        }

        #endregion

        #region DragStarted — фиксируем начальный прямоугольник

        private static void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            var presenter = FindPresenter(thumb);
            if (presenter == null)
                return;

            double x = GetLeft(presenter);
            double y = GetTop(presenter);
            double w = presenter.ActualWidth;
            double h = presenter.ActualHeight;

            if (w > 0 && h > 0)
                thumb.SetValue(DragInitialRectProperty, new Rect(x, y, w, h));
        }

        #endregion

        #region DragDelta — логика ресайза

        private static void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            var presenter    = FindPresenter(thumb);
            var itemsControl = FindAncestor<ItemsControl>(thumb);
            var canvas       = FindAncestor<Canvas>(thumb);

            if (presenter == null || itemsControl == null || canvas == null)
                return;

            if (presenter.DataContext is not FuelDispenserViewModel vm)
                return;

            // Используем vm-значения (а не presenter.Actual*), потому что Actual*
            // обновляется только после layout-прохода — позже, чем следующий DragDelta.
            // Если читать устаревший ActualWidth, «правый край» при Left-resize уплывает
            // на несколько пикселей с каждым событием.
            double x      = vm.X;
            double y      = vm.Y;
            double width  = vm.Width;
            double height = vm.Height > 0 ? vm.Height : (presenter.ActualHeight > 0 ? presenter.ActualHeight : 1);

            if (width <= 0)
                return;

            var currentRect = new Rect(x, y, width, height);
            var initialRect = (Rect)thumb.GetValue(DragInitialRectProperty);

            double minWidth  = vm.MinWidth;
            double minHeight = vm.MinHeight;

            double newX      = x;
            double newY      = y;
            double newWidth  = width;
            double newHeight = height;

            switch (GetDirection(thumb))
            {
                case ResizeDirection.Right:
                {
                    newWidth = Math.Max(minWidth,
                                   Math.Min(width + e.HorizontalChange,
                                            canvas.ActualWidth - x));
                    break;
                }

                case ResizeDirection.Left:
                {
                    double right = x + width;
                    newWidth = Math.Max(minWidth, width - e.HorizontalChange);
                    newX     = right - newWidth;
                    if (newX < 0) { newX = 0; newWidth = right; }
                    break;
                }

                case ResizeDirection.Bottom:
                {
                    newHeight = Math.Max(minHeight,
                                    Math.Min(height + e.VerticalChange,
                                             canvas.ActualHeight - y));
                    break;
                }

                case ResizeDirection.Top:
                {
                    double bottom = y + height;
                    newHeight = Math.Max(minHeight, height - e.VerticalChange);
                    newY      = bottom - newHeight;
                    if (newY < 0) { newY = 0; newHeight = bottom; }
                    break;
                }

                default:
                    return;
            }

            var newRect = new Rect(newX, newY, newWidth, newHeight);

            // Разрешаем движение, только если оно не создаёт НОВЫХ пересечений
            // (т.е. с элементами, которые уже не пересекались в начале операции).
            Rect checkBase = initialRect.IsEmpty ? currentRect : initialRect;
            if (CreatesNewIntersection(itemsControl, presenter, newRect, checkBase))
                return;

            vm.X      = newX;
            vm.Y      = newY;
            vm.Width  = newWidth;
            vm.Height = newHeight;
        }

        #endregion

        #region Intersection helpers

        /// <summary>
        /// Возвращает true, если <paramref name="newRect"/> пересекается с элементом,
        /// который <paramref name="baseRect"/> НЕ пересекал.
        /// Это позволяет двигать карточку, даже если она уже перекрывается с другой
        /// (например, после загрузки из сохранённых позиций).
        /// </summary>
        private static bool CreatesNewIntersection(
            ItemsControl itemsControl,
            FrameworkElement movingPresenter,
            Rect newRect,
            Rect baseRect)
        {
            foreach (var item in itemsControl.Items)
            {
                var other = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (other == null || other == movingPresenter)
                    continue;

                double ox = GetLeft(other);
                double oy = GetTop(other);
                double ow = other.ActualWidth;
                double oh = other.ActualHeight;

                if (ow <= 0 || oh <= 0)
                    continue;

                var otherRect = new Rect(ox, oy, ow, oh);

                bool alreadyIntersecting = baseRect.IsEmpty
                    ? false
                    : baseRect.IntersectsWith(otherRect);

                bool wouldIntersect = newRect.IntersectsWith(otherRect);

                if (!alreadyIntersecting && wouldIntersect)
                    return true;
            }

            return false;
        }

        #endregion

        #region Utility

        private static FrameworkElement? FindPresenter(Thumb thumb) =>
            (FrameworkElement?)FindAncestor<ListBoxItem>(thumb)
            ?? FindAncestor<ContentPresenter>(thumb);

        private static double GetLeft(FrameworkElement el)
        {
            var v = Canvas.GetLeft(el);
            return double.IsNaN(v) ? 0 : v;
        }

        private static double GetTop(FrameworkElement el)
        {
            var v = Canvas.GetTop(el);
            return double.IsNaN(v) ? 0 : v;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T found)
                    return found;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        #endregion
    }
}
