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
    }
}
