using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Records
{
    public partial class StaffTrackingView : UserControl
    {
        private DateTime? _customRangeStartDate;
        private DateTime? _customRangeEndDate;
        private bool _isCustomRangeComplete;

        public StaffTrackingView()
        {
            InitializeComponent();
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
            if (sender is not MenuItem menuItem || DataContext is not StaffTrackingViewModel viewModel)
            {
                return;
            }

            var selectedFilter = menuItem.Header?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedFilter))
            {
                viewModel.SelectedDateFilter = selectedFilter;
            }
        }

        private void CustomDateRangePopup_Opened(object sender, EventArgs e)
        {
            if (DataContext is not StaffTrackingViewModel viewModel)
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
            if (DataContext is not StaffTrackingViewModel viewModel || !_customRangeStartDate.HasValue)
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

        private void KpiProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ProgressBar progressBar)
            {
                return;
            }

            var row = FindAncestor<DataGridRow>(progressBar);
            var delay = 80 + Math.Max(0, row?.GetIndex() ?? 0) * 45;
            AnimateProgress(progressBar, 480, delay);
        }

        private void PerformanceBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateScaleY(element, 560, 100);
            }
        }

        private void DeadlineSegment_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                AnimateDeadlineSegment(element, 140);
            }
        }

        private void TargetProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ProgressBar progressBar)
            {
                AnimateProgress(progressBar, 560, 180);
            }
        }

        private void TargetProgressBar_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (sender is ProgressBar progressBar && progressBar.IsLoaded)
            {
                AnimateProgress(progressBar, 480, 40);
            }
        }

        private static void AnimateProgress(ProgressBar progressBar, int durationMilliseconds, int delayMilliseconds)
        {
            progressBar.BeginAnimation(RangeBase.ValueProperty, null);
            var targetValue = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, progressBar.Value));
            progressBar.BeginAnimation(
                RangeBase.ValueProperty,
                new DoubleAnimation(progressBar.Minimum, targetValue, TimeSpan.FromMilliseconds(durationMilliseconds))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                });
        }

        private static void AnimateScaleY(FrameworkElement element, int durationMilliseconds, int delayMilliseconds)
        {
            var transform = element.RenderTransform as ScaleTransform;
            if (transform == null || transform.IsFrozen)
            {
                transform = new ScaleTransform(1, 1);
                element.RenderTransform = transform;
            }

            transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            transform.ScaleY = 0;
            transform.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMilliseconds))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        private static void AnimateDeadlineSegment(FrameworkElement element, int delayMilliseconds)
        {
            var transform = element.RenderTransform as RotateTransform;
            if (transform == null || transform.IsFrozen)
            {
                transform = new RotateTransform();
                element.RenderTransform = transform;
            }

            element.BeginAnimation(OpacityProperty, null);
            transform.BeginAnimation(RotateTransform.AngleProperty, null);
            element.Opacity = 0;
            transform.Angle = -45;

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = easing
                });
            transform.BeginAnimation(
                RotateTransform.AngleProperty,
                new DoubleAnimation(-45, 0, TimeSpan.FromMilliseconds(520))
                {
                    BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                    EasingFunction = easing
                });
        }

        private static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
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
