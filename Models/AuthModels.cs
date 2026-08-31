namespace QuanLyHoSo.Models
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Officer = "Officer";
        public const string Leader = "Leader";
    }

    public sealed class AppUser : ViewModels.ViewModelBase
    {
        private bool _isActive = true;

        public int Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string RoleText => Role switch
        {
            UserRoles.Admin => "Admin",
            UserRoles.Leader => "Lãnh đạo",
            _ => "Cán bộ"
        };
    }
}
