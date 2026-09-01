using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        public RecordInputView()
        {
            InitializeComponent();
        }

        private void AreaDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            AreaContextMenu.PlacementTarget = AreaDropDownButton;
            AreaContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AreaOptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem ||
                menuItem.DataContext is not AreaSelectionOption option ||
                option.IsGroup ||
                string.IsNullOrWhiteSpace(option.FilterValue) ||
                DataContext is not RecordInputViewModel viewModel)
            {
                return;
            }

            viewModel.AreaName = option.FilterValue;
            viewModel.AreaSearchText = option.DisplayName;
            AreaContextMenu.IsOpen = false;
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
