using System.Collections.ObjectModel;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ExportViewModel : ViewModelBase
    {
        public ExportViewModel()
        {
            var dataService = AppDataService.Instance;

            Statuses = new ObservableCollection<string>
            {
                "Tất cả",
                "Mới tiếp nhận",
                "Đang phân loại",
                "Đã phân công",
                "Đang xác minh",
                "Chờ kết quả",
                "Đang chờ bổ sung tài liệu",
                "Đã giải quyết",
                "Chuyển cơ quan khác"
            };
            CaseTypes = new ObservableCollection<string>(dataService.GetCatalogValues("CaseType", includeAll: true));
            Fields = new ObservableCollection<string>(dataService.GetCatalogValues("Field", includeAll: true));
            Areas = new ObservableCollection<string>(dataService.GetAreaNames(includeAll: true));
            Processors = new ObservableCollection<string>(dataService.GetProcessorNames(includeAll: true));
            SortOptions = new ObservableCollection<string> { "Ngày tiếp nhận mới nhất trước", "Ngày tiếp nhận cũ nhất trước", "Trạng thái", "Địa bàn" };
            PreviewRecords = new ObservableCollection<ExportRecordPreview>(dataService.GetExportPreview());
            TotalRecordsText = $"Tổng số hồ sơ: {dataService.CountRecords()}";
            ResultRangeText = $"Hiển thị 1 - {PreviewRecords.Count} của {dataService.CountRecords()} kết quả";
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> Areas { get; }
        public ObservableCollection<string> Processors { get; }
        public ObservableCollection<string> SortOptions { get; }
        public ObservableCollection<ExportRecordPreview> PreviewRecords { get; }
        public string TotalRecordsText { get; }
        public string ResultRangeText { get; }
    }
}
