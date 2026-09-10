using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Auth
{
    public partial class RequiredPasswordChangeView : UserControl
    {
        public RequiredPasswordChangeView()
        {
            InitializeComponent();
        }

        private void RequiredPasswordChangeView_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentPasswordInput.Focus();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is not RequiredPasswordChangeViewModel viewModel)
            {
                return;
            }

            viewModel.CurrentPassword = CurrentPasswordInput.Password;
            viewModel.NewPassword = NewPasswordInput.Password;
            viewModel.ConfirmPassword = ConfirmPasswordInput.Password;
        }

        private void ConfirmPasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not RequiredPasswordChangeViewModel viewModel)
            {
                return;
            }

            if (viewModel.ChangePasswordCommand.CanExecute(null))
            {
                viewModel.ChangePasswordCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
