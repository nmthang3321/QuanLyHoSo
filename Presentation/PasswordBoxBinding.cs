using System.Windows;
using System.Windows.Controls;

namespace QuanLyHoSo.Presentation
{
    /// <summary>
    /// Synchronizes a PasswordBox with a view-model property while keeping the
    /// password masked by the native WPF control.
    /// </summary>
    public static class PasswordBoxBinding
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxBinding),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject element) =>
            (string)element.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject element, string value) =>
            element.SetValue(BoundPasswordProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not PasswordBox passwordBox)
            {
                return;
            }

            passwordBox.PasswordChanged -= OnPasswordChanged;
            var password = e.NewValue as string ?? string.Empty;
            if (passwordBox.Password != password)
            {
                passwordBox.Password = password;
            }

            passwordBox.PasswordChanged += OnPasswordChanged;
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetBoundPassword(passwordBox, passwordBox.Password);
            }
        }
    }
}
