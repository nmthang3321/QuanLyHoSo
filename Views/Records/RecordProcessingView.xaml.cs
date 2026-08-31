using System.Windows.Controls;
using System.Windows.Input;

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
    }
}
