namespace QuanLyHoSo.Models
{
    public sealed class SettingAction
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string IconGlyph { get; set; }
        public string AccentColor { get; set; }
    }

    public sealed class SoftwareInfo
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }
}
