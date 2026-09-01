using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        private DateTime? _customRangeStartDate;
        private DateTime? _customRangeEndDate;
        private bool _isCustomRangeComplete;
        private DashboardViewModel _viewModel;
        private bool _isAnimationQueued;
        private DateTime _lastAnimationStartedAt = DateTime.MinValue;

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
            Unloaded += DashboardView_Unloaded;
            DataContextChanged += DashboardView_DataContextChanged;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            QueueDashboardAnimation();
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= DashboardViewModel_PropertyChanged;
                _viewModel = null;
            }
        }

        private void DashboardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= DashboardViewModel_PropertyChanged;
            }

            _viewModel = e.NewValue as DashboardViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += DashboardViewModel_PropertyChanged;
            }

            QueueDashboardAnimation();
        }

        private void DashboardViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DashboardViewModel.TotalRecordsText) ||
                e.PropertyName == nameof(DashboardViewModel.TrendLinePoints))
            {
                QueueDashboardAnimation();
            }
        }

        private void OverviewChartsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 980)
            {
                Grid.SetColumn(StatusChartCard, 0);
                Grid.SetColumnSpan(StatusChartCard, 2);
                Grid.SetRow(StatusChartCard, 0);
                StatusChartCard.Margin = new Thickness(0, 0, 0, 14);

                Grid.SetColumn(AreaChartCard, 0);
                Grid.SetColumnSpan(AreaChartCard, 2);
                Grid.SetRow(AreaChartCard, 1);
                AreaChartCard.Margin = new Thickness(0);
                return;
            }

            Grid.SetColumn(StatusChartCard, 0);
            Grid.SetColumnSpan(StatusChartCard, 1);
            Grid.SetRow(StatusChartCard, 0);
            StatusChartCard.Margin = new Thickness(0, 0, 8, 0);

            Grid.SetColumn(AreaChartCard, 1);
            Grid.SetColumnSpan(AreaChartCard, 1);
            Grid.SetRow(AreaChartCard, 0);
            AreaChartCard.Margin = new Thickness(8, 0, 0, 0);
        }

        private void DateFilterButton_Click(object sender, RoutedEventArgs e)
        {
            DateFilterHost.ContextMenu.PlacementTarget = DateFilterHost;
            DateFilterHost.ContextMenu.Placement = PlacementMode.Bottom;
            DateFilterHost.ContextMenu.HorizontalOffset = 0;
            DateFilterHost.ContextMenu.VerticalOffset = 4;
            DateFilterHost.ContextMenu.IsOpen = true;
        }

        private void DateFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || DataContext is not DashboardViewModel viewModel)
            {
                return;
            }

            var selectedFilter = menuItem.Header?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedFilter))
            {
                viewModel.SelectedDateFilter = selectedFilter;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null)
                return;

            // Allow child controls (DataGrid, etc.) to handle the wheel event first
            if (e.Source is not ScrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void TrendChartScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            DashboardScrollViewer.ScrollToVerticalOffset(DashboardScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ChartShape_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateDonutSegment(element, 0);
            }
        }

        private void HorizontalBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateScale(element, "ScaleX", 0, 1, 480, 80);
            }
        }

        private void VerticalBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateScale(element, "ScaleY", 0, 1, 560, 80);
            }
        }

        private void TrendLinePath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateScale(element, "ScaleX", 0, 1, 620, 180);
                AnimateFade(element, 120, 320);
            }
        }

        private void CustomDateRangePopup_Opened(object sender, EventArgs e)
        {
            if (DataContext is not DashboardViewModel viewModel)
            {
                return;
            }

            CustomDateRangeCalendar.DisplayDate = (viewModel.FromDate ?? DateTime.Today).Date;
            _customRangeStartDate = null;
            _customRangeEndDate = null;
            _isCustomRangeComplete = false;
            ResetCustomCalendarSelection();
        }

        private void ResetCustomCalendarSelection()
        {
            CustomDateRangeCalendar.SelectedDates.Clear();
        }

        private void CustomDateRangeCalendar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dayButton = FindAncestor<CalendarDayButton>(e.OriginalSource as DependencyObject);
            if (dayButton?.DataContext is not DateTime clickedDate || dayButton.IsBlackedOut)
            {
                return;
            }

            clickedDate = clickedDate.Date;
            if (!_customRangeStartDate.HasValue || _isCustomRangeComplete)
            {
                _customRangeStartDate = clickedDate;
                _customRangeEndDate = clickedDate;
                _isCustomRangeComplete = false;
                UpdateCustomCalendarSelection(clickedDate, clickedDate);
            }
            else
            {
                _customRangeEndDate = clickedDate;
                _isCustomRangeComplete = true;

                var fromDate = _customRangeStartDate.Value <= clickedDate
                    ? _customRangeStartDate.Value
                    : clickedDate;
                var toDate = _customRangeStartDate.Value <= clickedDate
                    ? clickedDate
                    : _customRangeStartDate.Value;

                UpdateCustomCalendarSelection(fromDate, toDate);
            }

            e.Handled = true;
        }

        private void ApplyCustomDateRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DashboardViewModel viewModel)
            {
                return;
            }

            if (!_customRangeStartDate.HasValue)
            {
                return;
            }

            var endDate = _customRangeEndDate ?? _customRangeStartDate.Value;
            viewModel.FromDate = _customRangeStartDate.Value <= endDate
                ? _customRangeStartDate.Value
                : endDate;
            viewModel.ToDate = _customRangeStartDate.Value <= endDate
                ? endDate
                : _customRangeStartDate.Value;

            if (viewModel.ApplyFilterCommand.CanExecute(null))
            {
                viewModel.ApplyFilterCommand.Execute(null);
            }

            viewModel.IsCustomCalendarOpen = false;
        }

        private void UpdateCustomCalendarSelection(DateTime fromDate, DateTime toDate)
        {
            ResetCustomCalendarSelection();
            CustomDateRangeCalendar.SelectedDates.AddRange(fromDate, toDate);
        }

        private void QueueDashboardAnimation()
        {
            if (!IsLoaded || _isAnimationQueued || AuthContext.IsOfficer)
            {
                return;
            }

            _isAnimationQueued = true;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _isAnimationQueued = false;
                    PlayDashboardAnimation();
                }),
                DispatcherPriority.ContextIdle);
        }

        private void PlayDashboardAnimation()
        {
            if ((DateTime.Now - _lastAnimationStartedAt).TotalMilliseconds < 900)
            {
                return;
            }

            _lastAnimationStartedAt = DateTime.Now;
            AnimateEntrance(StatusChartCard, 0);
            AnimateEntrance(AreaChartCard, 80);
            AnimateEntrance(TrendChartCard, 150);

            foreach (var element in FindVisualChildren<Path>(StatusChartCard))
            {
                AnimateDonutSegment(element, 120);
            }

            foreach (var element in FindVisualChildren<Border>(AreaChartCard))
            {
                if (element.RenderTransform is ScaleTransform)
                {
                    AnimateScale(element, "ScaleX", 0, 1, 480, 120);
                }
            }

            foreach (var element in FindVisualChildren<Border>(TrendChartCard))
            {
                if (element.RenderTransform is ScaleTransform)
                {
                    AnimateScale(element, "ScaleY", 0, 1, 560, 160);
                }
            }

            AnimateScale(TrendLinePath, "ScaleX", 0, 1, 620, 260);
            AnimateFade(TrendLinePath, 220, 360);
        }

        private static void AnimateEntrance(FrameworkElement element, int delayMilliseconds)
        {
            if (element == null)
            {
                return;
            }

            var transform = element.RenderTransform as TranslateTransform;
            if (transform == null || transform.IsFrozen)
            {
                transform = new TranslateTransform();
                element.RenderTransform = transform;
            }

            element.Opacity = 0;
            transform.Y = 10;

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = easing
                });
            transform.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(360))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = easing
                });
        }

        private static void AnimateFade(UIElement element, int delayMilliseconds, int durationMilliseconds)
        {
            if (element == null)
            {
                return;
            }

            element.Opacity = 0;
            element.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMilliseconds))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
        }

        private static void AnimateDonutSegment(FrameworkElement element, int delayMilliseconds)
        {
            if (element == null)
            {
                return;
            }

            var transform = element.RenderTransform as RotateTransform;
            if (transform == null || transform.IsFrozen)
            {
                transform = new RotateTransform();
                element.RenderTransform = transform;
            }

            element.BeginAnimation(OpacityProperty, null);
            transform.BeginAnimation(RotateTransform.AngleProperty, null);
            element.Opacity = 1;
            transform.Angle = -45;

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(
                RotateTransform.AngleProperty,
                new DoubleAnimation(-45, 0, TimeSpan.FromMilliseconds(520))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = easing
                });
        }

        private static void AnimateScale(FrameworkElement element, string propertyName, double from, double to, int durationMilliseconds, int delayMilliseconds)
        {
            if (element == null)
            {
                return;
            }

            var transform = element.RenderTransform as ScaleTransform;
            if (transform == null || transform.IsFrozen)
            {
                transform = new ScaleTransform(1, 1);
                element.RenderTransform = transform;
            }

            var dependencyProperty = propertyName == "ScaleY"
                ? ScaleTransform.ScaleYProperty
                : ScaleTransform.ScaleXProperty;

            transform.SetValue(dependencyProperty, from);
            transform.BeginAnimation(
                dependencyProperty,
                new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMilliseconds))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
