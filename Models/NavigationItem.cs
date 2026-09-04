using System.Windows.Input;

namespace QuanLyHoSo.Models
{
    public sealed class NavigationItem : ViewModels.ViewModelBase
    {
        private bool _isSelected;
        private bool _isVisible = true;
        private int _badgeCount;

        public string Key { get; set; }
        public string Title { get; set; }
        public string IconGlyph { get; set; }
        public string IconFontFamily { get; set; }
        public ICommand Command { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public int BadgeCount
        {
            get => _badgeCount;
            set
            {
                if (SetProperty(ref _badgeCount, value))
                {
                    OnPropertyChanged(nameof(HasBadge));
                    OnPropertyChanged(nameof(BadgeText));
                }
            }
        }

        public bool HasBadge => BadgeCount > 0;

        public string BadgeText => BadgeCount > 99 ? "99+" : BadgeCount.ToString();
    }
}
