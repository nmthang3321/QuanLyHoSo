using System.Windows;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ShellViewModel();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            (DataContext as System.IDisposable)?.Dispose();
            base.OnClosed(e);
        }
    }
}
