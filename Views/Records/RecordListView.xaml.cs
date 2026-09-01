using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordListView : UserControl
    {
        private readonly HashSet<string> _expandedAreaFilterGroups = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public RecordListView()
        {
            InitializeComponent();
        }

        private void RecordListScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && e.Source is not ScrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void RecordListDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            CloseAreaFilterPanel();
            RecordListScrollViewer.ScrollToVerticalOffset(RecordListScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void AreaFilterDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (AreaFilterPanel.Visibility == Visibility.Visible)
            {
                CloseAreaFilterPanel();
            }
            else
            {
                OpenAreaFilterPanel();
            }

            e.Handled = true;
        }

        private void OpenAreaFilterPanel()
        {
            PositionAreaFilterPanel();
            AreaFilterOverlayCanvas.IsHitTestVisible = true;
            AreaFilterPanel.Visibility = Visibility.Visible;
            AreaFilterSearchBox.Text = string.Empty;
            AreaFilterSearchHint.Visibility = Visibility.Visible;
            ExpandSelectedAreaFilterGroup();
            PopulateAreaFilterMenu((DataContext as RecordListViewModel)?.Areas);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AreaFilterSearchBox.Focus();
                Keyboard.Focus(AreaFilterSearchBox);
            }), DispatcherPriority.Input);
        }

        private void CloseAreaFilterPanel()
        {
            if (AreaFilterPanel != null)
            {
                AreaFilterPanel.Visibility = Visibility.Collapsed;
            }

            if (AreaFilterOverlayCanvas != null)
            {
                AreaFilterOverlayCanvas.IsHitTestVisible = false;
            }
        }

        private void PositionAreaFilterPanel()
        {
            AreaFilterPanel.Width = AreaFilterDropDownButton.ActualWidth;

            Point point;
            try
            {
                point = AreaFilterDropDownButton
                    .TransformToVisual(AreaFilterOverlayCanvas)
                    .Transform(new Point(0, AreaFilterDropDownButton.ActualHeight + 4));
            }
            catch (InvalidOperationException)
            {
                point = new Point(0, AreaFilterDropDownButton.ActualHeight + 4);
            }

            Canvas.SetLeft(AreaFilterPanel, point.X);
            Canvas.SetTop(AreaFilterPanel, point.Y);

            var availableHeight = AreaFilterOverlayCanvas.ActualHeight - point.Y - 12;
            AreaFilterPanel.MaxHeight = Math.Max(180, Math.Min(420, availableHeight));
        }

        private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AreaFilterPanel.Visibility != Visibility.Visible)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            if (IsWithin(source, AreaFilterPanel) || IsWithin(source, AreaFilterDropDownButton))
            {
                return;
            }

            CloseAreaFilterPanel();
        }

        private void RecordListScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (AreaFilterPanel.Visibility == Visibility.Visible)
            {
                CloseAreaFilterPanel();
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

        private void ExpandSelectedAreaFilterGroup()
        {
            if (DataContext is not RecordListViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.SelectedArea))
            {
                return;
            }

            var selectedGroup = AreaSelectionOptions.Flatten(viewModel.Areas)
                .FirstOrDefault(area => string.Equals(area.FilterValue, viewModel.SelectedArea, StringComparison.CurrentCultureIgnoreCase))
                ?.GroupName;

            if (!string.IsNullOrWhiteSpace(selectedGroup))
            {
                _expandedAreaFilterGroups.Add(selectedGroup);
            }
        }

        private void AreaFilterSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AreaFilterSearchHint.Visibility = string.IsNullOrEmpty(AreaFilterSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (DataContext is not RecordListViewModel viewModel)
            {
                return;
            }

            viewModel.AreaSearchText = AreaFilterSearchBox.Text;
            PopulateAreaFilterMenu(viewModel.FilteredAreas);
        }

        private void PopulateAreaFilterMenu(System.Collections.Generic.IEnumerable<AreaSelectionOption> options)
        {
            AreaFilterItemsControl.Items.Clear();

            if (options == null)
            {
                return;
            }

            var roots = options.ToList();
            var isSearching = DataContext is RecordListViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.AreaSearchText);
            foreach (var option in roots)
            {
                if (option.IsGroup)
                {
                    AreaFilterItemsControl.Items.Add(CreateAreaFilterGroupSection(option, isSearching));
                }
                else
                {
                    AreaFilterItemsControl.Items.Add(CreateAreaFilterItem(option));
                }
            }
        }

        private StackPanel CreateAreaFilterGroupSection(AreaSelectionOption group, bool isSearching)
        {
            var section = new StackPanel();
            section.Children.Add(CreateAreaFilterGroupHeader(group, isSearching));

            if (isSearching || _expandedAreaFilterGroups.Contains(group.DisplayName))
            {
                foreach (var child in group.Children)
                {
                    section.Children.Add(CreateAreaFilterItem(child, new Thickness(28, 7, 14, 7)));
                }
            }

            return section;
        }

        private Button CreateAreaFilterGroupHeader(AreaSelectionOption group, bool isSearching)
        {
            var isExpanded = isSearching || _expandedAreaFilterGroups.Contains(group.DisplayName);
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

            var button = CreateAreaFilterButton(row, group, new Thickness(14, 8, 14, 8));
            button.Click += AreaFilterGroupHeader_Click;
            return button;
        }

        private void AreaFilterGroupHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AreaSelectionOption group)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(group.FilterValue) && DataContext is RecordListViewModel viewModel)
            {
                viewModel.SelectedArea = group.FilterValue;
            }

            if (!_expandedAreaFilterGroups.Add(group.DisplayName))
            {
                _expandedAreaFilterGroups.Remove(group.DisplayName);
            }

            PopulateAreaFilterMenu((DataContext as RecordListViewModel)?.FilteredAreas);
            e.Handled = true;
        }

        private Button CreateAreaFilterItem(AreaSelectionOption option)
        {
            return CreateAreaFilterItem(option, new Thickness(14, 7, 14, 7));
        }

        private Button CreateAreaFilterItem(AreaSelectionOption option, Thickness padding)
        {
            var button = CreateAreaFilterButton(option.DisplayName, option, padding);
            button.Click += AreaFilterOptionButton_Click;
            return button;
        }

        private Button CreateAreaFilterButton(object content, AreaSelectionOption option, Thickness padding)
        {
            var button = new Button
            {
                Content = content,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = padding,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = option
            };

            var hoverStyle = new Style(typeof(Button));
            hoverStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            hoverStyle.Triggers.Add(trigger);
            button.Style = hoverStyle;

            return button;
        }

        private void AreaFilterOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AreaSelectionOption option ||
                string.IsNullOrWhiteSpace(option.FilterValue) ||
                DataContext is not RecordListViewModel viewModel)
            {
                return;
            }

            viewModel.SelectedArea = option.FilterValue;
            viewModel.AreaSearchText = option.DisplayName;
            CloseAreaFilterPanel();
            e.Handled = true;
        }
    }
}
