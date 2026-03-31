using KIT.GasStation.ViewModels;
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
    }

    public static class ResizeOnCanvasBehavior
    {
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

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Thumb thumb)
                return;

            if ((bool)e.NewValue)
                thumb.DragDelta += Thumb_DragDelta;
            else
                thumb.DragDelta -= Thumb_DragDelta;
        }

        private static void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not Thumb thumb)
                return;

            var presenter = FindAncestor<ContentPresenter>(thumb);
            var itemsControl = FindAncestor<ItemsControl>(thumb);
            var canvas = FindAncestor<Canvas>(thumb);

            if (presenter == null || itemsControl == null || canvas == null)
                return;

            if (presenter.DataContext is not FuelDispenserViewModel vm)
                return;

            double x = Canvas.GetLeft(presenter);
            double y = Canvas.GetTop(presenter);

            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            double width = presenter.ActualWidth;
            double height = presenter.ActualHeight;

            if (width <= 0)
                width = vm.Width;

            if (height <= 0)
                return;

            double minWidth = vm.MinWidth;
            double newX = x;
            double newWidth = width;

            switch (GetDirection(thumb))
            {
                case ResizeDirection.Right:
                    {
                        newWidth = width + e.HorizontalChange;

                        if (newWidth < minWidth)
                            newWidth = minWidth;

                        if (newX + newWidth > canvas.ActualWidth)
                            newWidth = canvas.ActualWidth - newX;

                        break;
                    }

                case ResizeDirection.Left:
                    {
                        double right = x + width;

                        newWidth = width - e.HorizontalChange;

                        if (newWidth < minWidth)
                            newWidth = minWidth;

                        newX = right - newWidth;

                        if (newX < 0)
                        {
                            newX = 0;
                            newWidth = right;
                        }

                        break;
                    }

                default:
                    return;
            }

            if (newWidth < minWidth)
                return;

            var newRect = new Rect(newX, y, newWidth, height);

            if (IntersectsAny(itemsControl, presenter, newRect))
                return;

            vm.X = newX;
            vm.Width = newWidth;
        }

        private static bool IntersectsAny(ItemsControl itemsControl, ContentPresenter currentPresenter, Rect rect)
        {
            foreach (var item in itemsControl.Items)
            {
                var other = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (other == null || other == currentPresenter)
                    continue;

                double otherX = Canvas.GetLeft(other);
                double otherY = Canvas.GetTop(other);

                if (double.IsNaN(otherX)) otherX = 0;
                if (double.IsNaN(otherY)) otherY = 0;

                double otherWidth = other.ActualWidth;
                double otherHeight = other.ActualHeight;

                if (otherWidth <= 0 || otherHeight <= 0)
                    continue;

                var otherRect = new Rect(otherX, otherY, otherWidth, otherHeight);

                if (rect.IntersectsWith(otherRect))
                    return true;
            }

            return false;
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
    }
}
