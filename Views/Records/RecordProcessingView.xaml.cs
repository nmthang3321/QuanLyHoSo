using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordProcessingView : UserControl
    {
        private const string CustomDateFilter = "Khác";
        private readonly HashSet<string> _expandedTransferAreaGroups = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        private DateTime? _customRangeStartDate;
        private DateTime? _customRangeEndDate;
        private bool _isCustomRangeComplete;
        private bool _openCustomDatePopupAfterMenuCloses;

        public RecordProcessingView()
        {
            InitializeComponent();
            Unloaded += RecordProcessingView_Unloaded;
        }

        private void RecordProcessingView_Unloaded(object sender, RoutedEventArgs e)
        {
            _openCustomDatePopupAfterMenuCloses = false;
            CustomDateRangePopup.IsOpen = false;
        }

        private void DateFilterButton_Click(object sender, RoutedEventArgs e)
        {
            _openCustomDatePopupAfterMenuCloses = false;
            CustomDateRangePopup.IsOpen = false;
            DateFilterHost.ContextMenu.PlacementTarget = DateFilterHost;
            DateFilterHost.ContextMenu.Placement = PlacementMode.Bottom;
            DateFilterHost.ContextMenu.HorizontalOffset = 0;
            DateFilterHost.ContextMenu.VerticalOffset = 4;
            DateFilterHost.ContextMenu.IsOpen = true;
        }

        private void DateFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || DataContext is not RecordProcessingViewModel viewModel)
            {
                return;
            }

            var selectedFilter = menuItem.Header?.ToString();
            if (string.IsNullOrWhiteSpace(selectedFilter))
            {
                return;
            }

            if (!string.Equals(selectedFilter, CustomDateFilter, StringComparison.Ordinal))
            {
                viewModel.SelectedDateFilter = selectedFilter;
                return;
            }

            _openCustomDatePopupAfterMenuCloses = true;
            viewModel.SelectedDateFilter = CustomDateFilter;
            DateFilterHost.ContextMenu.IsOpen = false;
        }

        private void DateFilterContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (!_openCustomDatePopupAfterMenuCloses)
            {
                return;
            }

            _openCustomDatePopupAfterMenuCloses = false;
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (IsLoaded)
                    {
                        CustomDateRangePopup.IsOpen = true;
                    }
                }),
                DispatcherPriority.ContextIdle);
        }

        private void CustomDateRangePopup_Opened(object sender, EventArgs e)
        {
            if (DataContext is not RecordProcessingViewModel viewModel)
            {
                return;
            }

            CustomDateRangeCalendar.DisplayDate = (viewModel.FromDate ?? DateTime.Today).Date;
            _customRangeStartDate = null;
            _customRangeEndDate = null;
            _isCustomRangeComplete = false;
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
                var fromDate = _customRangeStartDate.Value <= clickedDate ? _customRangeStartDate.Value : clickedDate;
                var toDate = _customRangeStartDate.Value <= clickedDate ? clickedDate : _customRangeStartDate.Value;
                UpdateCustomCalendarSelection(fromDate, toDate);
            }

            e.Handled = true;
        }

        private void ApplyCustomDateRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not RecordProcessingViewModel viewModel || !_customRangeStartDate.HasValue)
            {
                return;
            }

            var endDate = _customRangeEndDate ?? _customRangeStartDate.Value;
            viewModel.FromDate = _customRangeStartDate.Value <= endDate ? _customRangeStartDate.Value : endDate;
            viewModel.ToDate = _customRangeStartDate.Value <= endDate ? endDate : _customRangeStartDate.Value;
            if (viewModel.ApplyFilterCommand.CanExecute(null))
            {
                viewModel.ApplyFilterCommand.Execute(null);
            }

            CustomDateRangePopup.IsOpen = false;
            viewModel.IsCustomCalendarOpen = false;
        }

        private void UpdateCustomCalendarSelection(DateTime fromDate, DateTime toDate)
        {
            CustomDateRangeCalendar.SelectedDates.Clear();
            CustomDateRangeCalendar.SelectedDates.AddRange(fromDate, toDate);
        }

        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                {
                    return match;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private void SelectableTaskText_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.Cursor = textBox.IsMouseCaptured || textBox.GetCharacterIndexFromPoint(e.GetPosition(textBox), false) >= 0
                    ? Cursors.IBeam : Cursors.Hand;
        }

        private void SelectableTaskText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox
                && textBox.GetCharacterIndexFromPoint(e.GetPosition(textBox), false) < 0
                && DataContext is RecordProcessingViewModel vm
                && vm.ViewRecordCommand.CanExecute(textBox.DataContext))
            {
                e.Handled = true;
                vm.ViewRecordCommand.Execute(textBox.DataContext);
            }
        }

        private void TransferAreaDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (TransferAreaPanel.Visibility == Visibility.Visible)
            {
                CloseTransferAreaPanel();
            }
            else
            {
                OpenTransferAreaPanel();
            }

            e.Handled = true;
        }

        private void OpenTransferAreaPanel()
        {
            PositionTransferAreaPanel();
            TransferAreaOverlayCanvas.IsHitTestVisible = true;
            TransferAreaPanel.Visibility = Visibility.Visible;
            TransferAreaSearchBox.Text = string.Empty;
            TransferAreaSearchHint.Visibility = Visibility.Visible;
            ExpandSelectedTransferAreaGroup();
            PopulateTransferAreaMenu((DataContext as RecordProcessingViewModel)?.TransferAreas);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TransferAreaSearchBox.Focus();
                Keyboard.Focus(TransferAreaSearchBox);
            }), DispatcherPriority.Input);
        }

        private void CloseTransferAreaPanel()
        {
            TransferAreaPanel.Visibility = Visibility.Collapsed;
            TransferAreaOverlayCanvas.IsHitTestVisible = false;
        }

        private void PositionTransferAreaPanel()
        {
            TransferAreaPanel.Width = TransferAreaDropDownButton.ActualWidth;

            Point point;
            try
            {
                point = TransferAreaDropDownButton
                    .TransformToVisual(TransferAreaOverlayCanvas)
                    .Transform(new Point(0, TransferAreaDropDownButton.ActualHeight + 4));
            }
            catch (InvalidOperationException)
            {
                point = new Point(0, TransferAreaDropDownButton.ActualHeight + 4);
            }

            Canvas.SetLeft(TransferAreaPanel, point.X);
            Canvas.SetTop(TransferAreaPanel, point.Y);

            var availableHeight = TransferAreaOverlayCanvas.ActualHeight - point.Y - 12;
            TransferAreaPanel.MaxHeight = Math.Max(180, Math.Min(420, availableHeight));
        }

        private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TransferAreaPanel.Visibility != Visibility.Visible)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            if (IsWithin(source, TransferAreaPanel) || IsWithin(source, TransferAreaDropDownButton))
            {
                return;
            }

            CloseTransferAreaPanel();
        }

        private void ProcessingDetailScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (TransferAreaPanel.Visibility == Visibility.Visible)
            {
                CloseTransferAreaPanel();
            }
        }

        private static bool IsWithin(DependencyObject source, DependencyObject parent)
        {
            while (source != null)
            {
                if (ReferenceEquals(source, parent))
                {
                    return true;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private void ExpandSelectedTransferAreaGroup()
        {
            if (DataContext is not RecordProcessingViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.TransferAreaName))
            {
                return;
            }

            var selectedGroup = AreaSelectionOptions.Flatten(viewModel.TransferAreas)
                .FirstOrDefault(area => string.Equals(area.FilterValue, viewModel.TransferAreaName, StringComparison.CurrentCultureIgnoreCase))
                ?.GroupName;

            if (!string.IsNullOrWhiteSpace(selectedGroup))
            {
                _expandedTransferAreaGroups.Add(selectedGroup);
            }
        }

        private void TransferAreaSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TransferAreaSearchHint.Visibility = string.IsNullOrEmpty(TransferAreaSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (DataContext is not RecordProcessingViewModel viewModel)
            {
                return;
            }

            viewModel.TransferAreaSearchText = TransferAreaSearchBox.Text;
            PopulateTransferAreaMenu(viewModel.FilteredTransferAreas);
        }

        private void PopulateTransferAreaMenu(IEnumerable<AreaSelectionOption> options)
        {
            TransferAreaItemsControl.Items.Clear();

            if (options == null)
            {
                return;
            }

            var roots = options.ToList();
            var isSearching = DataContext is RecordProcessingViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.TransferAreaSearchText);
            foreach (var option in roots)
            {
                if (option.IsGroup)
                {
                    TransferAreaItemsControl.Items.Add(CreateTransferAreaGroupSection(option, isSearching));
                }
                else
                {
                    TransferAreaItemsControl.Items.Add(CreateTransferAreaItem(option));
                }
            }
        }

        private StackPanel CreateTransferAreaGroupSection(AreaSelectionOption group, bool isSearching)
        {
            var section = new StackPanel();
            section.Children.Add(CreateTransferAreaGroupHeader(group, isSearching));

            if (isSearching || _expandedTransferAreaGroups.Contains(group.DisplayName))
            {
                foreach (var child in group.Children)
                {
                    section.Children.Add(CreateTransferAreaItem(child, new Thickness(28, 7, 14, 7)));
                }
            }

            return section;
        }

        private Button CreateTransferAreaGroupHeader(AreaSelectionOption group, bool isSearching)
        {
            var isExpanded = isSearching || _expandedTransferAreaGroups.Contains(group.DisplayName);
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var chevron = new TextBlock
            {
                Text = isExpanded ? "\uE70D" : "\uE76C",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(chevron, 0);
            row.Children.Add(chevron);

            var title = new TextBlock
            {
                Text = group.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("StrongTextBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(title, 1);
            row.Children.Add(title);

            var count = new TextBlock
            {
                Text = group.Children.Count.ToString(),
                Foreground = (Brush)FindResource("MutedTextBrush"),
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(count, 2);
            row.Children.Add(count);

            var button = new Button
            {
                Content = row,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(14, 8, 14, 8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                Tag = group
            };

            var hoverStyle = new Style(typeof(Button));
            hoverStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            hoverStyle.Triggers.Add(trigger);
            button.Style = hoverStyle;

            button.Click += TransferAreaGroupHeader_Click;
            return button;
        }

        private void TransferAreaGroupHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not AreaSelectionOption group)
            {
                return;
            }

            if (!_expandedTransferAreaGroups.Add(group.DisplayName))
            {
                _expandedTransferAreaGroups.Remove(group.DisplayName);
            }

            PopulateTransferAreaMenu((DataContext as RecordProcessingViewModel)?.FilteredTransferAreas);
            e.Handled = true;
        }

        private Button CreateTransferAreaItem(AreaSelectionOption option)
        {
            return CreateTransferAreaItem(option, new Thickness(14, 7, 14, 7));
        }

        private Button CreateTransferAreaItem(AreaSelectionOption option, Thickness padding)
        {
            var button = new Button
            {
                Content = option.DisplayName,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = padding,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = option
            };

            var hoverStyle = new Style(typeof(Button));
            hoverStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            hoverStyle.Triggers.Add(trigger);
            button.Style = hoverStyle;

            button.Click += TransferAreaOptionButton_Click;
            return button;
        }

        private void TransferAreaOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AreaSelectionOption option ||
                string.IsNullOrWhiteSpace(option.FilterValue) ||
                DataContext is not RecordProcessingViewModel viewModel)
            {
                return;
            }

            viewModel.TransferAreaName = option.FilterValue;
            viewModel.TransferAreaSearchText = option.DisplayName;
            CloseTransferAreaPanel();
            e.Handled = true;
        }

        private void ProcessingQueueDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ProcessingQueueScrollViewer.ScrollToVerticalOffset(ProcessingQueueScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ChooseProcessingAttachmentFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Tài liệu hỗ trợ (*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png)|*.pdf;*.doc;*.docx;*.jpg;*.jpeg;*.png",
                Multiselect = true,
                Title = "Chọn tài liệu đính kèm"
            };
            if (dialog.ShowDialog() == true && DataContext is RecordProcessingViewModel viewModel)
            {
                viewModel.AddAttachmentFiles(dialog.FileNames);
            }
        }

        private void ProcessingAttachmentDropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is RecordProcessingViewModel viewModel)
            {
                viewModel.AddAttachmentFiles((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }
    }
}
