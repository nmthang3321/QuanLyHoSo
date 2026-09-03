namespace QuanLyHoSo.Models
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Officer = "Officer";
        public const string Leader = "Leader";
    }

    public sealed class AppUser : INotifyPropertyChanged
    {
        private bool _isActive = true;

        public int Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                {
                    return;
                }

                _isActive = value;
                OnPropertyChanged();
            }
        }

        public string RoleText => Role switch
        {
            UserRoles.Admin => "Admin",
            UserRoles.Leader => "Lãnh đạo",
            _ => "Cán bộ"
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
