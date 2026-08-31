using System;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private readonly Action<AppUser> _onSignedIn;
        private string _userName;
        private string _errorMessage;

        public LoginViewModel(Action<AppUser> onSignedIn)
        {
            _dataService = AppDataService.Instance;
            _onSignedIn = onSignedIn ?? (_ => { });
            SignInCommand = new RelayCommand(SignIn);
            UserName = "admin";
        }

        public ICommand SignInCommand { get; }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password { private get; set; }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        private void SignIn()
        {
            try
            {
                var user = _dataService.AuthenticateUser(UserName, Password);
                if (user == null)
                {
                    ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return;
                }

                ErrorMessage = string.Empty;
                AppLogger.Info("Auth", "SignIn", $"User signed in: {user.UserName}.");
                _onSignedIn(user);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "SignIn", ex, "Failed to sign in.");
                ErrorMessage = "Không thể đăng nhập. Vui lòng thử lại.";
                MessageBox.Show(ex.Message, "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
