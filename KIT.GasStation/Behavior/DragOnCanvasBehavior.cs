using KIT.GasStation.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace KIT.GasStation.Behavior
{
    public static class DragOnCanvasBehavior
    {
        // Насколько близко надо подтащить, чтобы сработало прилипание
        private const double HorizontalSnapDistance = 14;
        private const double VerticalSnapDistance = 2;

        // Допуск по второй оси, чтобы snap не срабатывал через полэкрана
        private const double OrthogonalSnapTolerance = 16;

        // Зазор между элементами
        private const double Gap = 2;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DragOnCanvasBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsEnabledProperty, value);

        private static readonly DependencyProperty DragStateProperty =
            DependencyProperty.RegisterAttached(
                "DragState",
                typeof(DragState),
                typeof(DragOnCanvasBehavior),
                new PropertyMetadata(null));

        private static DragState? GetDragState(DependencyObject obj) =>
            (DragState?)obj.GetValue(DragStateProperty);

        private static void SetDragState(DependencyObject obj, DragState? value) =>
            obj.SetValue(DragStateProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            if ((bool)e.NewValue)
            {
                element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                element.MouseMove += Element_MouseMove;
                element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                element.LostMouseCapture += Element_LostMouseCapture;
            }
            else
            {
                element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
                element.MouseMove -= Element_MouseMove;
                element.MouseLeftButtonUp -= Element_MouseLeftButtonUp;
                element.LostMouseCapture -= Element_LostMouseCapture;
            }
        }

        private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            // Если клик был по Thumb — drag не стартуем
            if (e.OriginalSource is DependencyObject source)
            {
                var thumb = FindAncestorOrSelf<Thumb>(source);
                if (thumb != null)
                    return;
            }

            var presenter = FindAncestor<ContentPresenter>(element);
            var canvas = FindAncestor<Canvas>(element);

            if (presenter == null || canvas == null)
                return;

            double left = Canvas.GetLeft(presenter);
            double top = Canvas.GetTop(presenter);

            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            SetDragState(element, new DragState
            {
                IsDragging = true,
                Presenter = presenter,
                Canvas = canvas,
                StartMousePosition = e.GetPosition(canvas),
                StartLeft = left,
                StartTop = top
            });

            element.CaptureMouse();
            e.Handled = true;
        }

        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            var state = GetDragState(element);
            if (state == null || !state.IsDragging || state.Presenter == null || state.Canvas == null)
                return;

            var presenter = state.Presenter;
            var canvas = state.Canvas;
            var itemsControl = FindAncestor<ItemsControl>(presenter);

            if (itemsControl == null)
                return;

            double width = presenter.ActualWidth;
            double height = presenter.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            Point mousePosition = e.GetPosition(canvas);
            double dx = mousePosition.X - state.StartMousePosition.X;
            double dy = mousePosition.Y - state.StartMousePosition.Y;

            double desiredX = state.StartLeft + dx;
            double desiredY = state.StartTop + dy;

            desiredX = ClampX(desiredX, width, canvas.ActualWidth);
            desiredY = ClampY(desiredY, height, canvas.ActualHeight);

            var desiredRect = new Rect(desiredX, desiredY, width, height);

            // Пытаемся найти лучшую snap-позицию
            SnapCandidate? snapCandidate = FindBestSnap(
                itemsControl,
                presenter,
                desiredRect,
                canvas.ActualWidth,
                canvas.ActualHeight);

            double finalX = desiredX;
            double finalY = desiredY;

            if (snapCandidate != null)
            {
                finalX = snapCandidate.X;
                finalY = snapCandidate.Y;
            }

            finalX = ClampX(finalX, width, canvas.ActualWidth);
            finalY = ClampY(finalY, height, canvas.ActualHeight);

            var finalRect = new Rect(finalX, finalY, width, height);

            // Если snapped-позиция пересекается — пробуем обычную
            if (IntersectsAny(itemsControl, presenter, finalRect))
            {
                if (!IntersectsAny(itemsControl, presenter, desiredRect))
                {
                    finalX = desiredX;
                    finalY = desiredY;
                }
                else
                {
                    // И snapped, и desired пересекаются — не двигаем
                    return;
                }
            }

            Canvas.SetLeft(presenter, finalX);
            Canvas.SetTop(presenter, finalY);

            if (presenter.DataContext is FuelDispenserViewModel vm)
            {
                vm.X = finalX;
                vm.Y = finalY;
            }
        }

        private static void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            StopDragging(element);
            e.Handled = true;
        }

        private static void Element_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement element)
                return;

            StopDragging(element);
        }

        private static void StopDragging(FrameworkElement element)
        {
            var state = GetDragState(element);
            if (state != null)
                state.IsDragging = false;

            if (Mouse.Captured == element)
                element.ReleaseMouseCapture();
        }

        private static SnapCandidate? FindBestSnap(
            ItemsControl itemsControl,
            ContentPresenter movingPresenter,
            Rect desiredRect,
            double canvasWidth,
            double canvasHeight)
        {
            SnapCandidate? best = null;

            foreach (var item in itemsControl.Items)
            {
                var otherPresenter = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (otherPresenter == null || otherPresenter == movingPresenter)
                    continue;

                var otherRect = GetPresenterRect(otherPresenter);
                if (otherRect == null)
                    continue;

                foreach (var candidate in BuildCandidates(otherRect.Value, desiredRect.Width, desiredRect.Height))
                {
                    if (!IsNear(desiredRect, otherRect.Value, candidate.Side))
                        continue;

                    double snappedX = ClampX(candidate.X, desiredRect.Width, canvasWidth);
                    double snappedY = ClampY(candidate.Y, desiredRect.Height, canvasHeight);

                    var snappedRect = new Rect(snappedX, snappedY, desiredRect.Width, desiredRect.Height);

                    if (IntersectsAny(itemsControl, movingPresenter, snappedRect))
                        continue;

                    double score = DistanceSquared(desiredRect.Left, desiredRect.Top, snappedX, snappedY);

                    if (best == null || score < best.Score)
                    {
                        best = new SnapCandidate
                        {
                            X = snappedX,
                            Y = snappedY,
                            Score = score
                        };
                    }
                }
            }

            return best;
        }

        private static IEnumerable<CandidatePosition> BuildCandidates(Rect target, double movingWidth, double movingHeight)
        {
            // Слева от target
            yield return new CandidatePosition
            {
                Side = SnapSide.Left,
                X = target.Left - movingWidth - Gap,
                Y = target.Top
            };

            yield return new CandidatePosition
            {
                Side = SnapSide.Left,
                X = target.Left - movingWidth - Gap,
                Y = target.Bottom - movingHeight
            };

            // Справа от target
            yield return new CandidatePosition
            {
                Side = SnapSide.Right,
                X = target.Right + Gap,
                Y = target.Top
            };

            yield return new CandidatePosition
            {
                Side = SnapSide.Right,
                X = target.Right + Gap,
                Y = target.Bottom - movingHeight
            };

            // Сверху над target
            yield return new CandidatePosition
            {
                Side = SnapSide.Top,
                X = target.Left,
                Y = target.Top - movingHeight - Gap
            };

            yield return new CandidatePosition
            {
                Side = SnapSide.Top,
                X = target.Right - movingWidth,
                Y = target.Top - movingHeight - Gap
            };

            // Снизу под target
            yield return new CandidatePosition
            {
                Side = SnapSide.Bottom,
                X = target.Left,
                Y = target.Bottom + Gap
            };

            yield return new CandidatePosition
            {
                Side = SnapSide.Bottom,
                X = target.Right - movingWidth,
                Y = target.Bottom + Gap
            };
        }

        private static bool IsNear(Rect movingRect, Rect targetRect, SnapSide side)
        {
            return side switch
            {
                SnapSide.Left => IsNearLeft(movingRect, targetRect),
                SnapSide.Right => IsNearRight(movingRect, targetRect),
                SnapSide.Top => IsNearTop(movingRect, targetRect),
                SnapSide.Bottom => IsNearBottom(movingRect, targetRect),
                _ => false
            };
        }

        private static bool IsNearLeft(Rect movingRect, Rect targetRect)
        {
            double horizontalDistance = Math.Abs(movingRect.Right - targetRect.Left);

            bool verticallyRelated = RangesOverlapOrClose(
                movingRect.Top, movingRect.Bottom,
                targetRect.Top, targetRect.Bottom,
                OrthogonalSnapTolerance);

            return horizontalDistance <= HorizontalSnapDistance && verticallyRelated;
        }

        private static bool IsNearRight(Rect movingRect, Rect targetRect)
        {
            double horizontalDistance = Math.Abs(movingRect.Left - targetRect.Right);

            bool verticallyRelated = RangesOverlapOrClose(
                movingRect.Top, movingRect.Bottom,
                targetRect.Top, targetRect.Bottom,
                OrthogonalSnapTolerance);

            return horizontalDistance <= HorizontalSnapDistance && verticallyRelated;
        }

        private static bool IsNearTop(Rect movingRect, Rect targetRect)
        {
            double verticalDistance = Math.Abs(movingRect.Bottom - targetRect.Top);

            bool horizontallyRelated = RangesOverlapOrClose(
                movingRect.Left, movingRect.Right,
                targetRect.Left, targetRect.Right,
                OrthogonalSnapTolerance);

            return verticalDistance <= VerticalSnapDistance && horizontallyRelated;
        }

        private static bool IsNearBottom(Rect movingRect, Rect targetRect)
        {
            double verticalDistance = Math.Abs(movingRect.Top - targetRect.Bottom);

            bool horizontallyRelated = RangesOverlapOrClose(
                movingRect.Left, movingRect.Right,
                targetRect.Left, targetRect.Right,
                OrthogonalSnapTolerance);

            return verticalDistance <= VerticalSnapDistance && horizontallyRelated;
        }

        private static bool RangesOverlapOrClose(
            double aStart,
            double aEnd,
            double bStart,
            double bEnd,
            double tolerance)
        {
            // Пересекаются
            if (aEnd >= bStart && bEnd >= aStart)
                return true;

            // Или почти касаются
            if (Math.Abs(aEnd - bStart) <= tolerance)
                return true;

            if (Math.Abs(bEnd - aStart) <= tolerance)
                return true;

            return false;
        }

        private static bool IntersectsAny(
            ItemsControl itemsControl,
            ContentPresenter movingPresenter,
            Rect movingRect)
        {
            foreach (var item in itemsControl.Items)
            {
                var otherPresenter = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (otherPresenter == null || otherPresenter == movingPresenter)
                    continue;

                var otherRect = GetPresenterRect(otherPresenter);
                if (otherRect == null)
                    continue;

                if (movingRect.IntersectsWith(otherRect.Value))
                    return true;
            }

            return false;
        }

        private static Rect? GetPresenterRect(ContentPresenter presenter)
        {
            double x = Canvas.GetLeft(presenter);
            double y = Canvas.GetTop(presenter);

            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            double width = presenter.ActualWidth;
            double height = presenter.ActualHeight;

            if (width <= 0 || height <= 0)
                return null;

            return new Rect(x, y, width, height);
        }

        private static double ClampX(double x, double width, double canvasWidth)
        {
            if (canvasWidth <= 0)
                return Math.Max(0, x);

            return Math.Max(0, Math.Min(x, canvasWidth - width));
        }

        private static double ClampY(double y, double height, double canvasHeight)
        {
            if (canvasHeight <= 0)
                return Math.Max(0, y);

            return Math.Max(0, Math.Min(y, canvasHeight - height));
        }

        private static double DistanceSquared(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return dx * dx + dy * dy;
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

        private static T? FindAncestorOrSelf<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T found)
                    return found;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private sealed class DragState
        {
            public bool IsDragging { get; set; }
            public Point StartMousePosition { get; set; }
            public double StartLeft { get; set; }
            public double StartTop { get; set; }
            public ContentPresenter? Presenter { get; set; }
            public Canvas? Canvas { get; set; }
        }

        private sealed class SnapCandidate
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Score { get; set; }
        }

        private sealed class CandidatePosition
        {
            public SnapSide Side { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        private enum SnapSide
        {
            Left,
            Right,
            Top,
            Bottom
        }
    }
}
