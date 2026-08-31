namespace QuanLyHoSo.Models
{
    public sealed class CatalogGroupSetting : ViewModels.ViewModelBase
    {
        private int _itemCount;
        private bool _isSelected;

        public string CatalogType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconGlyph { get; set; }
        public string AccentColor { get; set; }
        public string IconBackground { get; set; }

        public int ItemCount
        {
            get => _itemCount;
            set => SetProperty(ref _itemCount, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public sealed class CatalogValueSetting : ViewModels.ViewModelBase
    {
        private int _displayOrder;

        public int Id { get; set; }
        public string CatalogType { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;

        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }
    }

    public sealed class SoftwareInfo
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public sealed class SystemLogEntry
    {
        public int Index { get; set; }
        public string CreatedAt { get; set; }
        public string UserName { get; set; }
        public string Module { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string Detail { get; set; }
    }
}
