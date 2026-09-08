using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuanLyHoSo.ViewModels;

namespace QuanLyHoSo.Views.Auth
{
    public partial class LoginView : UserControl
    {
        private bool _isPasswordVisible;
        private bool _isSyncingPassword;
        private bool _hasPlayedEntrance;

        public LoginView()
        {
            InitializeComponent();
            UpdateUserNamePlaceholder();
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_hasPlayedEntrance)
            {
                _hasPlayedEntrance = true;
                if (SystemParameters.ClientAreaAnimation)
                {
                    AnimateEntrance(LoginIntroduction);
                    AnimateEntrance(LoginForm);
                }
            }

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

        private static void AnimateEntrance(FrameworkElement element)
        {
            var duration = TimeSpan.FromMilliseconds(320);
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration)
            {
                FillBehavior = FillBehavior.Stop
            });
            ((TranslateTransform)element.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(12, 0, duration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                });
        }

        private void LoginField_MouseChanged(object sender, MouseEventArgs e)
        {
            UpdateLoginFieldAppearance((Border)sender);
        }

        private void LoginField_FocusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateLoginFieldAppearance((Border)sender);
        }

        private static void UpdateLoginFieldAppearance(Border field)
        {
            var borderColor = field.IsKeyboardFocusWithin
                ? Color.FromRgb(11, 92, 255)
                : field.IsMouseOver ? Color.FromRgb(164, 183, 212) : Color.FromRgb(213, 222, 235);
            var backgroundColor = field.IsKeyboardFocusWithin ? Colors.White : Color.FromRgb(248, 250, 253);
            AnimateFieldBrush(field, Border.BorderBrushProperty, borderColor);
            AnimateFieldBrush(field, Border.BackgroundProperty, backgroundColor);
        }

        private static void AnimateFieldBrush(Border field, DependencyProperty property, Color targetColor)
        {
            var currentColor = (field.GetValue(property) as SolidColorBrush)?.Color ?? targetColor;
            // Each field owns its brush so animations never modify shared style resources.
            var brush = new SolidColorBrush(targetColor);
            field.SetValue(property, brush);
            if (SystemParameters.ClientAreaAnimation && currentColor != targetColor)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty,
                    new ColorAnimation(currentColor, targetColor, TimeSpan.FromMilliseconds(140))
                    {
                        FillBehavior = FillBehavior.Stop
                    });
            }
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
