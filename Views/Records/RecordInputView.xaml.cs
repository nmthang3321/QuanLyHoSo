using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QuanLyHoSo.Models;
using Microsoft.Win32;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordInputView : UserControl
    {
        private static readonly Brush DropZoneNormalBackground = Brushes.White;
        private static readonly Brush DropZoneNormalBorder = new SolidColorBrush(Color.FromRgb(127, 174, 255));
        private static readonly Brush DropZoneHighlightBackground = new SolidColorBrush(Color.FromRgb(239, 246, 255));
        private static readonly Brush DropZoneHighlightBorder = new SolidColorBrush(Color.FromRgb(11, 92, 255));
        private readonly HashSet<string> _expandedAreaGroups = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public RecordInputView()
        {
            InitializeComponent();
        }

        private void AreaDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (AreaPanel.Visibility == Visibility.Visible)
            {
                CloseAreaPanel();
            }
            else
            {
                OpenAreaPanel();
            }

            e.Handled = true;
        }

        private void OpenAreaPanel()
        {
            PositionAreaPanel();
            AreaOverlayCanvas.IsHitTestVisible = true;
            AreaPanel.Visibility = Visibility.Visible;
            AreaSearchBox.Text = string.Empty;
            AreaSearchHint.Visibility = Visibility.Visible;
            ExpandSelectedAreaGroup();
            PopulateAreaMenu((DataContext as RecordInputViewModel)?.Areas);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AreaSearchBox.Focus();
                Keyboard.Focus(AreaSearchBox);
            }), DispatcherPriority.Input);
        }

        private void CloseAreaPanel()
        {
            AreaPanel.Visibility = Visibility.Collapsed;
            AreaOverlayCanvas.IsHitTestVisible = false;
        }

        private void PositionAreaPanel()
        {
            AreaPanel.Width = AreaDropDownButton.ActualWidth;

            Point point;
            try
            {
                point = AreaDropDownButton
                    .TransformToVisual(AreaOverlayCanvas)
                    .Transform(new Point(0, AreaDropDownButton.ActualHeight + 4));
            }
            catch (InvalidOperationException)
            {
                point = new Point(0, AreaDropDownButton.ActualHeight + 4);
            }

            Canvas.SetLeft(AreaPanel, point.X);
            Canvas.SetTop(AreaPanel, point.Y);

            var availableHeight = AreaOverlayCanvas.ActualHeight - point.Y - 12;
            AreaPanel.MaxHeight = Math.Max(180, Math.Min(420, availableHeight));
        }

        private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AreaPanel.Visibility != Visibility.Visible)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            if (IsWithin(source, AreaPanel) || IsWithin(source, AreaDropDownButton))
            {
                return;
            }

            CloseAreaPanel();
        }

        private void InputScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (AreaPanel.Visibility == Visibility.Visible)
            {
                CloseAreaPanel();
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

        private void ExpandSelectedAreaGroup()
        {
            if (DataContext is not RecordInputViewModel viewModel || string.IsNullOrWhiteSpace(viewModel.AreaName))
            {
                return;
            }

            var selectedGroup = AreaSelectionOptions.Flatten(viewModel.Areas)
                .FirstOrDefault(area => string.Equals(area.FilterValue, viewModel.AreaName, StringComparison.CurrentCultureIgnoreCase))
                ?.GroupName;

            if (!string.IsNullOrWhiteSpace(selectedGroup))
            {
                _expandedAreaGroups.Add(selectedGroup);
            }
        }

        private void AreaSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AreaSearchHint.Visibility = string.IsNullOrEmpty(AreaSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (DataContext is not RecordInputViewModel viewModel)
            {
                return;
            }

            viewModel.AreaSearchText = AreaSearchBox.Text;
            PopulateAreaMenu(viewModel.FilteredAreas);
        }

        private void PopulateAreaMenu(System.Collections.Generic.IEnumerable<AreaSelectionOption> options)
        {
            AreaItemsControl.Items.Clear();

            if (options == null)
            {
                return;
            }

            var roots = options.ToList();
            var isSearching = DataContext is RecordInputViewModel viewModel && !string.IsNullOrWhiteSpace(viewModel.AreaSearchText);
            foreach (var option in roots)
            {
                if (option.IsGroup)
                {
                    AreaItemsControl.Items.Add(CreateGroupSection(option, isSearching));
                }
                else
                {
                    AreaItemsControl.Items.Add(CreateAreaItem(option));
                }
            }
        }

        private StackPanel CreateGroupSection(AreaSelectionOption group, bool isSearching)
        {
            var section = new StackPanel();
            section.Children.Add(CreateGroupHeader(group, isSearching));

            if (isSearching || _expandedAreaGroups.Contains(group.DisplayName))
            {
                foreach (var child in group.Children)
                {
                    section.Children.Add(CreateAreaItem(child, new Thickness(28, 7, 14, 7)));
                }
            }

            return section;
        }

        private Button CreateGroupHeader(AreaSelectionOption group, bool isSearching)
        {
            var isExpanded = isSearching || _expandedAreaGroups.Contains(group.DisplayName);
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
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.SemiBold,
                Tag = group
            };

            var hoverStyle = new Style(typeof(Button));
            hoverStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 246, 255))));
            hoverStyle.Triggers.Add(trigger);
            button.Style = hoverStyle;

            button.Click += AreaGroupHeader_Click;
            return button;
        }

        private void AreaGroupHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AreaSelectionOption group)
            {
                return;
            }

            if (!_expandedAreaGroups.Add(group.DisplayName))
            {
                _expandedAreaGroups.Remove(group.DisplayName);
            }

            PopulateAreaMenu((DataContext as RecordInputViewModel)?.FilteredAreas);
            e.Handled = true;
        }

        private Button CreateAreaItem(AreaSelectionOption option)
        {
            return CreateAreaItem(option, new Thickness(14, 7, 14, 7));
        }

        private Button CreateAreaItem(AreaSelectionOption option, Thickness padding)
        {
            var button = new Button
            {
                Content = option.DisplayName,
                HorizontalContentAlignment = HorizontalAlignment.Left,
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

            button.Click += AreaOptionButton_Click;
            return button;
        }

        private void AreaOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not AreaSelectionOption option ||
                string.IsNullOrWhiteSpace(option.FilterValue) ||
                DataContext is not RecordInputViewModel viewModel)
            {
                return;
            }

            viewModel.AreaName = option.FilterValue;
            viewModel.AreaSearchText = option.DisplayName;
            CloseAreaPanel();
            e.Handled = true;
        }

        private void ChooseAttachmentFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Tài liệu hỗ trợ (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png",
                Multiselect = true,
                Title = "Chọn tài liệu đính kèm"
            };

            if (dialog.ShowDialog() == true)
            {
                AddAttachmentFiles(dialog.FileNames);
            }
        }

        private void AttachmentDropZone_DragEnter(object sender, DragEventArgs e)
        {
            UpdateDropEffect(e);
        }

        private void AttachmentDropZone_PreviewDragEnter(object sender, DragEventArgs e)
        {
            UpdateDropEffect(e);
        }

        private void AttachmentDropZone_DragOver(object sender, DragEventArgs e)
        {
            UpdateDropEffect(e);
        }

        private void AttachmentDropZone_DragLeave(object sender, DragEventArgs e)
        {
            SetAttachmentDropHighlight(false);
        }

        private void AttachmentDropZone_PreviewDragLeave(object sender, DragEventArgs e)
        {
            SetAttachmentDropHighlight(false);
        }

        private void AttachmentDropZone_PreviewDragOver(object sender, DragEventArgs e)
        {
            UpdateDropEffect(e);
        }

        private void AttachmentDropZone_Drop(object sender, DragEventArgs e)
        {
            DropAttachmentFiles(e);
        }

        private void AttachmentDropZone_PreviewDrop(object sender, DragEventArgs e)
        {
            DropAttachmentFiles(e);
        }

        private void AddAttachmentFiles(string[] fileNames)
        {
            if (DataContext is RecordInputViewModel viewModel)
            {
                viewModel.AddAttachmentFiles(fileNames);
            }
        }

        private void UpdateDropEffect(DragEventArgs e)
        {
            var hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
            SetAttachmentDropHighlight(hasFiles);
            e.Handled = true;
        }

        private void DropAttachmentFiles(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                AddAttachmentFiles((string[])e.Data.GetData(DataFormats.FileDrop));
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            SetAttachmentDropHighlight(false);
            e.Handled = true;
        }

        private void SetAttachmentDropHighlight(bool isHighlighted)
        {
            AttachmentDropZone.Background = isHighlighted ? DropZoneHighlightBackground : DropZoneNormalBackground;
            AttachmentDropZone.BorderBrush = isHighlighted ? DropZoneHighlightBorder : DropZoneNormalBorder;
            AttachmentDropZone.BorderThickness = isHighlighted ? new Thickness(2) : new Thickness(1);
        }
    }
}
