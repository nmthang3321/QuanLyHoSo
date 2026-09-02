using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private readonly Action<AppUser> _onSignedIn;
        private string _userName;
        private string _errorMessage;
        private bool _rememberMe;

        public LoginViewModel(Action<AppUser> onSignedIn)
        {
            _dataService = AppDataService.Instance;
            _onSignedIn = onSignedIn ?? (_ => { });
            SignInCommand = new RelayCommand(SignIn);
            ForgotPasswordCommand = new RelayCommand(ShowForgotPasswordMessage);
            UserName = string.Empty;
            LoadRememberedLogin();
        }

        public ICommand SignInCommand { get; }

        public ICommand ForgotPasswordCommand { get; }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Password { get; set; }

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (SetProperty(ref _rememberMe, value) && !value)
                {
                    ClearRememberedLogin();
                }
            }
        }

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

        private static string RememberFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Settings",
                "login-remember.json");

        private void SignIn()
        {
            try
            {
                var user = _dataService.AuthenticateUser(UserName, Password);
                if (user == null)
                {
                    ErrorMessage = "T\u00EAn \u0111\u0103ng nh\u1EADp ho\u1EB7c m\u1EADt kh\u1EA9u kh\u00F4ng \u0111\u00FAng.";
                    return;
                }

                ErrorMessage = string.Empty;
                SaveRememberedLogin();
                AppLogger.Info("Auth", "SignIn", $"User signed in: {user.UserName}.");
                _onSignedIn(user);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "SignIn", ex, "Failed to sign in.");
                ErrorMessage = ex is LanServerUnavailableException
                    ? "Kh\u00F4ng k\u1EBFt n\u1ED1i \u0111\u01B0\u1EE3c m\u00E1y server/admin."
                    : "Kh\u00F4ng th\u1EC3 \u0111\u0103ng nh\u1EADp. Vui l\u00F2ng th\u1EED l\u1EA1i.";
                MessageBox.Show(ex.Message, "L\u1ED7i \u0111\u0103ng nh\u1EADp", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRememberedLogin()
        {
            try
            {
                if (!File.Exists(RememberFilePath))
                {
                    return;
                }

                var json = File.ReadAllText(RememberFilePath, Encoding.UTF8);
                var remembered = JsonSerializer.Deserialize<RememberedLogin>(json);
                if (remembered?.RememberMe != true)
                {
                    return;
                }

                RememberMe = true;
                UserName = remembered.UserName ?? string.Empty;
                Password = Decode(remembered.PasswordText);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "LoadRememberedLogin", ex, "Failed to load remembered login.");
            }
        }

        private void SaveRememberedLogin()
        {
            if (!RememberMe)
            {
                ClearRememberedLogin();
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RememberFilePath));
                var remembered = new RememberedLogin
                {
                    RememberMe = true,
                    UserName = UserName ?? string.Empty,
                    PasswordText = Encode(Password ?? string.Empty)
                };
                var json = JsonSerializer.Serialize(remembered, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RememberFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "SaveRememberedLogin", ex, "Failed to save remembered login.");
            }
        }

        private static void ClearRememberedLogin()
        {
            try
            {
                if (File.Exists(RememberFilePath))
                {
                    File.Delete(RememberFilePath);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "ClearRememberedLogin", ex, "Failed to clear remembered login.");
            }
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        private static void ShowForgotPasswordMessage()
        {
            MessageBox.Show(
                "Vui l\u00F2ng li\u00EAn h\u1EC7 admin \u0111\u1EC3 c\u1EA5p l\u1EA1i m\u1EADt kh\u1EA9u.",
                "Qu\u00EAn m\u1EADt kh\u1EA9u",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private sealed class RememberedLogin
        {
            public bool RememberMe { get; set; }

            public string UserName { get; set; }

            public string PasswordText { get; set; }
        }
    }
}
