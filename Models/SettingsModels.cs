namespace QuanLyHoSo.Models
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public sealed class CatalogGroupSetting : INotifyPropertyChanged
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
            set
            {
                if (_itemCount == value)
                {
                    return;
                }

                _itemCount = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class CatalogValueSetting : INotifyPropertyChanged
    {
        private int _displayOrder;

        public int Id { get; set; }
        public string CatalogType { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;

        public int DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (_displayOrder == value)
                {
                    return;
                }

                _displayOrder = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    public sealed class InternalUpdatePackageInfo
    {
        public bool HasPackage { get; set; }
        public string Version { get; set; }
        public string FileName { get; set; }
        public long SizeBytes { get; set; }
        public string PublishedAt { get; set; }
    }
}
