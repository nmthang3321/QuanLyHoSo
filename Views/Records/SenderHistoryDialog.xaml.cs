using System.Windows.Controls;
using System.Windows;

namespace QuanLyHoSo.Views.Records
{
    public partial class SenderHistoryDialog : UserControl
    {
        public SenderHistoryDialog() => InitializeComponent();

        private void OnLoaded(object sender, RoutedEventArgs e) => HistoryGrid.Focus();
    }
}
