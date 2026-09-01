using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;

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

        private void AreaFilterDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            AreaFilterContextMenu.PlacementTarget = AreaFilterDropDownButton;
            AreaFilterContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AreaFilterOptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem ||
                menuItem.DataContext is not AreaSelectionOption option ||
                string.IsNullOrWhiteSpace(option.FilterValue) ||
                DataContext is not RecordListViewModel viewModel)
            {
                return;
            }

            viewModel.SelectedArea = option.FilterValue;
            AreaFilterContextMenu.IsOpen = false;
            e.Handled = true;
        }
    }
}
