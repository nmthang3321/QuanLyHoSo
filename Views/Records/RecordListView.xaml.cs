using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyHoSo.Views.Records
{
    public partial class RecordListView : UserControl
    {
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
            RecordListScrollViewer.ScrollToVerticalOffset(RecordListScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
