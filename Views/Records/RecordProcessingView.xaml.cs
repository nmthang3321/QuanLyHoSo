using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordProcessingView : UserControl
    {
        public RecordProcessingView()
        {
            InitializeComponent();
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
