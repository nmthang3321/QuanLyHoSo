using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        private DateTime? _customRangeStartDate;
        private DateTime? _customRangeEndDate;
        private bool _isCustomRangeComplete;

        public DashboardView()
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
