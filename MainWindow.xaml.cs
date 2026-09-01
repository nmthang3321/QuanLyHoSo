using System.Windows;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = AppPathSettings.Current.IsClientMode
                ? "Phần mềm quản lý hồ sơ [CLIENT]"
                : "Phần mềm quản lý hồ sơ [SERVER]";
            DataContext = new ShellViewModel();
        }
    }
}
