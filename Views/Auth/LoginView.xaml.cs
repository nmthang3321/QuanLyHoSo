using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Auth
{
    public partial class LoginView : UserControl
    {
        private bool _isPasswordVisible;
        private bool _isSyncingPassword;

        public LoginView()
        {
            InitializeComponent();
            UpdateUserNamePlaceholder();
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && !string.IsNullOrEmpty(viewModel.Password))
            {
                _isSyncingPassword = true;
                PasswordInput.Password = viewModel.Password;
                PasswordVisibleInput.Text = viewModel.Password;
                _isSyncingPassword = false;
                UpdatePasswordPlaceholder(viewModel.Password);
            }

            UpdateUserNamePlaceholder();
        }

        private void UserNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUserNamePlaceholder();
        }

        private void UserNameInput_FocusChanged(object sender, RoutedEventArgs e)
        {
            UpdateUserNamePlaceholder();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
                SyncVisiblePassword(passwordBox.Password);
                UpdatePasswordPlaceholder(passwordBox.Password);
            }
        }

        private void PasswordVisibleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword || sender is not TextBox textBox)
            {
                return;
            }

            _isSyncingPassword = true;
            PasswordInput.Password = textBox.Text;
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = textBox.Text;
            }
            UpdatePasswordPlaceholder(textBox.Text);
            _isSyncingPassword = false;
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                SyncVisiblePassword(PasswordInput.Password);
                PasswordInput.Visibility = Visibility.Collapsed;
                PasswordVisibleInput.Visibility = Visibility.Visible;
                TogglePasswordSlash.Visibility = Visibility.Collapsed;
                TogglePasswordButton.ToolTip = "\u1EA8n m\u1EADt kh\u1EA9u";
                UpdatePasswordPlaceholder(PasswordVisibleInput.Text);
                PasswordVisibleInput.Focus();
                PasswordVisibleInput.CaretIndex = PasswordVisibleInput.Text.Length;
                return;
            }

            PasswordVisibleInput.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            TogglePasswordSlash.Visibility = Visibility.Visible;
            TogglePasswordButton.ToolTip = "Hi\u1EC7n m\u1EADt kh\u1EA9u";
            UpdatePasswordPlaceholder(PasswordInput.Password);
            PasswordInput.Focus();
        }

        private void PasswordField_FocusChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder(_isPasswordVisible ? PasswordVisibleInput.Text : PasswordInput.Password);
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not LoginViewModel viewModel)
            {
                return;
            }

            if (viewModel.SignInCommand.CanExecute(null))
            {
                viewModel.SignInCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void SyncVisiblePassword(string password)
        {
            if (_isSyncingPassword || PasswordVisibleInput.Text == password)
            {
                return;
            }

            _isSyncingPassword = true;
            PasswordVisibleInput.Text = password;
            _isSyncingPassword = false;
        }

        private void UpdatePasswordPlaceholder(string password)
        {
            var isFocused = PasswordInput.IsKeyboardFocusWithin || PasswordVisibleInput.IsKeyboardFocusWithin;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(password) && !isFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void UpdateUserNamePlaceholder()
        {
            UserNamePlaceholder.Visibility = string.IsNullOrEmpty(UserNameInput.Text) && !UserNameInput.IsKeyboardFocusWithin
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
