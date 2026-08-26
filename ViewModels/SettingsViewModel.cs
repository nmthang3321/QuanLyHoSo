using System.Collections.ObjectModel;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            AreaActions = new ObservableCollection<SettingAction>
            {
                new SettingAction { Title = "Tổng số xã/phường", Value = "102", IconGlyph = "\uE707", AccentColor = "#0B5CFF" },
                new SettingAction { Title = "Thêm mới địa bàn", IconGlyph = "\uE710", AccentColor = "#0B5CFF" },
                new SettingAction { Title = "Sửa thông tin địa bàn", IconGlyph = "\uE70F", AccentColor = "#0B5CFF" },
                new SettingAction { Title = "Xóa địa bàn", IconGlyph = "\uE74D", AccentColor = "#E11414" }
            };

            CatalogActions = new ObservableCollection<SettingAction>
            {
                new SettingAction { Title = "Loại vụ việc", IconGlyph = "\uE8A5", AccentColor = "#1FA24A" },
                new SettingAction { Title = "Lĩnh vực", IconGlyph = "\uE8F9", AccentColor = "#1FA24A" },
                new SettingAction { Title = "Nhóm nội dung", IconGlyph = "\uE8FD", AccentColor = "#1FA24A" },
                new SettingAction { Title = "Nguồn tiếp nhận", IconGlyph = "\uE77B", AccentColor = "#1FA24A" }
            };

            SoftwareInfos = new ObservableCollection<SoftwareInfo>
            {
                new SoftwareInfo { Label = "Phiên bản", Value = "1.0.0" },
                new SoftwareInfo { Label = "Khu vực", Value = "An Giang" },
                new SoftwareInfo { Label = "Cơ sở dữ liệu", Value = "SQLite (Local)" },
                new SoftwareInfo { Label = "Ngày cập nhật", Value = "25/08/2026" }
            };
        }

        public ObservableCollection<SettingAction> AreaActions { get; }
        public ObservableCollection<SettingAction> CatalogActions { get; }
        public ObservableCollection<SoftwareInfo> SoftwareInfos { get; }
    }
}

