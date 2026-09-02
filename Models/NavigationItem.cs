using System.Windows.Input;

namespace QuanLyHoSo.Models
{
    public sealed class NavigationItem : ViewModels.ViewModelBase
    {
        private bool _isSelected;

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
    }
}
