using System;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Network;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RequiredPasswordChangeViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private readonly AppUser _user;
        private readonly Action<AppUser> _onCompleted;
        private readonly Action _onCancel;
        private string _errorMessage;

        public RequiredPasswordChangeViewModel(AppUser user, Action<AppUser> onCompleted, Action onCancel)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
            _onCancel = onCancel ?? throw new ArgumentNullException(nameof(onCancel));
            _dataService = AppDataService.Instance;
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            CancelCommand = new RelayCommand(_onCancel);
        }

        public ICommand ChangePasswordCommand { get; }
        public ICommand CancelCommand { get; }
        public string UserDisplayText => $"{_user.DisplayName} ({_user.UserName})";
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

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

        private void ChangePassword()
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.";
                return;
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu mới cần tối thiểu 6 ký tự.";
                return;
            }

            if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
            {
                ErrorMessage = "Mật khẩu mới nhập lại chưa khớp.";
                return;
            }

            if (string.Equals(CurrentPassword, NewPassword, StringComparison.Ordinal))
            {
                ErrorMessage = "Mật khẩu mới cần khác mật khẩu tạm hoặc mật khẩu hiện tại.";
                return;
            }

            try
            {
                if (!_dataService.ChangeCurrentUserPassword(CurrentPassword, NewPassword))
                {
                    ErrorMessage = "Mật khẩu hiện tại không đúng. Vui lòng kiểm tra và thử lại.";
                    return;
                }

                ErrorMessage = string.Empty;
                _user.MustChangePassword = false;
                AppLogger.Info("Auth", "CompleteRequiredPasswordChange", $"User completed required password change: {_user.UserName}.");
                _onCompleted(_user);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Auth", "RequiredPasswordChange", ex, "Failed to complete required password change.", _user.UserName);
                ErrorMessage = ex is LanServerUnavailableException
                    ? "Không kết nối được máy server. Vui lòng kiểm tra kết nối và thử lại."
                    : "Không thể đổi mật khẩu. Vui lòng thử lại.";
            }
        }
    }
}
