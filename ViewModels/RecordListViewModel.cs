using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using Microsoft.Win32;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordListViewModel : ViewModelBase
    {
        private const int DefaultPageSize = 20;
        private const int MinimumPageSize = 1;
        private const int MaximumPageSize = 20;

        private readonly AppDataService _dataService;
        private readonly Action _goBack;
        private readonly Action<string> _editRecord;
        private readonly Action<string> _classifyRecord;
        private readonly RelayCommand _nextPageCommand;
        private readonly RelayCommand _previousPageCommand;
        private readonly RelayCommand _refreshCommand;
        private string _areaSearchText;
        private DateTime? _fromDate;
        private string _searchText;
        private string _selectedArea;
        private string _selectedCaseType;
        private string _selectedField;
        private string _selectedProcessor;
        private string _selectedSortOption;
        private string _selectedStatus;
        private bool _isFilterPanelOpen;
        private bool _isExporting;
        private int _currentPage = 1;
        private int _pageSize = DefaultPageSize;
        private string _pageSizeText = DefaultPageSize.ToString(CultureInfo.InvariantCulture);
        private RecordFormDraft _selectedRecordDetail;
        private DateTime? _toDate;
        private int _totalPages = 1;
        private string _totalRecordsText;

        public RecordListViewModel(Action goBack, Action<string> editRecord, Action<string> classifyRecord)
        {
            _dataService = AppDataService.Instance;
            _goBack = goBack ?? (() => { });
            _editRecord = editRecord ?? (_ => { });
            _classifyRecord = classifyRecord ?? (_ => { });
            Records = new ObservableCollection<RecordListRowViewModel>();

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
            CaseTypes = new ObservableCollection<string>(_dataService.GetCatalogValues("CaseType", includeAll: true));
            Fields = new ObservableCollection<string>(_dataService.GetCatalogValues("Field", includeAll: true));
            Areas = AreaSelectionOptions.Build(_dataService.GetAreaNames(includeAll: true), includeGroupRows: true, groupRowsSelectable: true);
            FilteredAreas = AreaSelectionOptions.Filter(Areas, null);
            Processors = new ObservableCollection<string>(_dataService.GetProcessorNames(includeAll: true));
            _dataService.CatalogChanged += DataService_CatalogChanged;
            SortOptions = new ObservableCollection<string> { "Ngày tiếp nhận mới nhất trước", "Ngày tiếp nhận cũ nhất trước", "Trạng thái", "Địa bàn" };

            _previousPageCommand = new RelayCommand(PreviousPage, () => CurrentPage > 1);
            _nextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            _refreshCommand = new RelayCommand(ReloadFromFirstPage);
            ApplyFilterCommand = new RelayCommand(ReloadFromFirstPage);
            ResetFilterCommand = new RelayCommand(ResetFilters);
            ExportCommand = new RelayCommand(async () => await ExportExcelAsync(), () => !_isExporting);
            BackCommand = new RelayCommand(_goBack);
            CloseDetailCommand = new RelayCommand(CloseDetail);

            ResetFilters();
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<AreaSelectionOption> Areas { get; }
        public ObservableCollection<AreaSelectionOption> FilteredAreas { get; }
        public ObservableCollection<string> Processors { get; }
        public ObservableCollection<string> SortOptions { get; }
        public ObservableCollection<RecordListRowViewModel> Records { get; }
        public ICommand PreviousPageCommand => _previousPageCommand;
        public ICommand NextPageCommand => _nextPageCommand;
        public ICommand RefreshCommand => _refreshCommand;
        public ICommand ApplyFilterCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CloseDetailCommand { get; }

        public bool IsFilterPanelOpen
        {
            get => _isFilterPanelOpen;
            set => SetProperty(ref _isFilterPanelOpen, value);
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public string SelectedCaseType
        {
            get => _selectedCaseType;
            set => SetProperty(ref _selectedCaseType, value);
        }

        public string SelectedField
        {
            get => _selectedField;
            set => SetProperty(ref _selectedField, value);
        }

        public string SelectedArea
        {
            get => _selectedArea;
            set
            {
                if (SetProperty(ref _selectedArea, value))
                {
                    var displayName = SelectedAreaDisplayName;
                    if (!string.Equals(_areaSearchText, displayName, StringComparison.Ordinal))
                    {
                        _areaSearchText = displayName;
                        OnPropertyChanged(nameof(AreaSearchText));
                        ReplaceAreaOptions(FilteredAreas, AreaSelectionOptions.Filter(Areas, displayName));
                    }

                    OnPropertyChanged(nameof(SelectedAreaDisplayName));
                }
            }
        }

        public string SelectedAreaDisplayName => AreaSelectionOptions.GetDisplayName(Areas, SelectedArea);

        public string AreaSearchText
        {
            get => _areaSearchText;
            set
            {
                if (!SetProperty(ref _areaSearchText, value))
                {
                    return;
                }

                ReplaceAreaOptions(FilteredAreas, AreaSelectionOptions.Filter(Areas, value));

                var exactMatch = AreaSelectionOptions.Flatten(Areas).FirstOrDefault(area => area.IsSelectable && string.Equals(area.DisplayName, value, StringComparison.CurrentCultureIgnoreCase));
                if (exactMatch != null && !string.Equals(_selectedArea, exactMatch.FilterValue, StringComparison.Ordinal))
                {
                    _selectedArea = exactMatch.FilterValue;
                    OnPropertyChanged(nameof(SelectedArea));
                    OnPropertyChanged(nameof(SelectedAreaDisplayName));
                }
            }
        }

        public string SelectedProcessor
        {
            get => _selectedProcessor;
            set => SetProperty(ref _selectedProcessor, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set => SetProperty(ref _selectedSortOption, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(PageText));
                    RaisePageCommandStates();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    OnPropertyChanged(nameof(PageText));
                    RaisePageCommandStates();
                }
            }
        }

        public string PageText => $"Trang {CurrentPage}/{TotalPages}";

        public string PageSizeText
        {
            get => _pageSizeText;
            set
            {
                if (!SetProperty(ref _pageSizeText, value))
                {
                    return;
                }

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageSize))
                {
                    return;
                }

                pageSize = Math.Max(MinimumPageSize, Math.Min(MaximumPageSize, pageSize));
                var normalizedPageSizeText = pageSize.ToString(CultureInfo.InvariantCulture);
                if (!string.Equals(_pageSizeText, normalizedPageSizeText, StringComparison.Ordinal))
                {
                    _pageSizeText = normalizedPageSizeText;
                    OnPropertyChanged(nameof(PageSizeText));
                }

                if (_pageSize == pageSize)
                {
                    return;
                }

                _pageSize = pageSize;
                OnPropertyChanged(nameof(TableHeight));
                ReloadFromFirstPage();
            }
        }

        public string TotalRecordsText
        {
            get => _totalRecordsText;
            private set => SetProperty(ref _totalRecordsText, value);
        }

        public int TableHeight => 38 + _pageSize * 34;

        public RecordFormDraft SelectedRecordDetail
        {
            get => _selectedRecordDetail;
            private set
            {
                if (SetProperty(ref _selectedRecordDetail, value))
                {
                    OnPropertyChanged(nameof(IsDetailOpen));
                }
            }
        }

        public bool IsDetailOpen => SelectedRecordDetail != null;

        public void Reload()
        {
            var totalRecords = _dataService.CountFilteredRecords(
                FromDate,
                ToDate,
                SelectedStatus,
                SelectedCaseType,
                SelectedField,
                SelectedArea,
                SelectedProcessor,
                SearchText);
            TotalRecordsText = $"{totalRecords:N0} hồ sơ";
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)_pageSize));
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            LoadPage();
        }

        private void ReloadFromFirstPage()
        {
            CurrentPage = 1;
            Reload();
        }

        private void NextPage()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }

            CurrentPage++;
            LoadPage();
        }

        private void PreviousPage()
        {
            if (CurrentPage <= 1)
            {
                return;
            }

            CurrentPage--;
            LoadPage();
        }

        private void LoadPage()
        {
            var skip = (CurrentPage - 1) * _pageSize;
            var records = _dataService.GetFilteredRecords(
                FromDate,
                ToDate,
                SelectedStatus,
                SelectedCaseType,
                SelectedField,
                SelectedArea,
                SelectedProcessor,
                SearchText,
                SelectedSortOption,
                _pageSize,
                skip);
            var rows = new List<RecordListRowViewModel>();
            var index = skip + 1;
            foreach (var record in records)
            {
                record.Index = index++;
                rows.Add(new RecordListRowViewModel(
                    record,
                    new RelayCommand(() => ViewRecord(record.RecordCode)),
                    new RelayCommand(() => EditRecord(record.RecordCode)),
                    new RelayCommand(() => ClassifyRecord(record.RecordCode)),
                    new RelayCommand(() => DeleteRecord(record.RecordCode))));
            }

            Records.Clear();
            foreach (var row in rows)
            {
                Records.Add(row);
            }

            RaisePageCommandStates();
        }

        private void ResetFilters()
        {
            var today = DateTime.Today;
            FromDate = new DateTime(today.Year, today.Month, 1);
            ToDate = today;
            SelectedStatus = GetFirstOrDefault(Statuses);
            SelectedCaseType = GetFirstOrDefault(CaseTypes);
            SelectedField = GetFirstOrDefault(Fields);
            SelectedArea = GetFirstOrDefault(Areas);
            AreaSearchText = SelectedAreaDisplayName;
            SelectedProcessor = GetFirstOrDefault(Processors);
            SearchText = string.Empty;
            SelectedSortOption = GetFirstOrDefault(SortOptions);
            ReloadFromFirstPage();
        }

        private void DataService_CatalogChanged(string catalogType)
        {
            var shouldReload = false;
            switch (catalogType)
            {
                case "CaseType":
                    shouldReload = RefreshFilterCatalog(CaseTypes, _dataService.GetCatalogValues(catalogType, includeAll: true), SelectedCaseType, value => SelectedCaseType = value);
                    break;
                case "Field":
                    shouldReload = RefreshFilterCatalog(Fields, _dataService.GetCatalogValues(catalogType, includeAll: true), SelectedField, value => SelectedField = value);
                    break;
                case "ProcessorName":
                    shouldReload = RefreshFilterCatalog(Processors, _dataService.GetProcessorNames(includeAll: true), SelectedProcessor, value => SelectedProcessor = value);
                    break;
            }

            if (shouldReload)
            {
                ReloadFromFirstPage();
            }
        }

        private static bool RefreshFilterCatalog(ObservableCollection<string> target, IReadOnlyList<string> source, string selectedValue, Action<string> setSelectedValue)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }

            if (string.IsNullOrWhiteSpace(selectedValue) || target.Contains(selectedValue))
            {
                return false;
            }

            setSelectedValue(GetFirstOrDefault(target));
            return true;
        }

        private async Task ExportExcelAsync()
        {
            ReloadFromFirstPage();

            var fromDate = FromDate;
            var toDate = ToDate;
            var selectedStatus = SelectedStatus;
            var selectedCaseType = SelectedCaseType;
            var selectedField = SelectedField;
            var selectedArea = SelectedArea;
            var selectedProcessor = SelectedProcessor;
            var searchText = SearchText;
            var selectedSortOption = SelectedSortOption;

            IReadOnlyList<ExportRecordPreview> records;
            try
            {
                SetExporting(true);
                records = await Task.Run(() => _dataService.GetExportPreview(
                    fromDate,
                    toDate,
                    selectedStatus,
                    selectedCaseType,
                    selectedField,
                    selectedArea,
                    selectedProcessor,
                    searchText,
                    selectedSortOption,
                    take: int.MaxValue));
            }
            catch (Exception ex)
            {
                AppLogger.Error("Export", "LoadExportRecords", ex, "Failed to load records for export.");
                MessageBox.Show($"Không thể tải dữ liệu xuất. Vui lòng thử lại.\n\nChi tiết: {ex.Message}", "Lỗi xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                SetExporting(false);
            }

            if (records.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu phù hợp với bộ lọc hiện tại.", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "xlsx",
                FileName = $"QuanLyHoSo_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                OverwritePrompt = true,
                Title = "Chọn nơi lưu file xuất dữ liệu"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                SetExporting(true);
                var fileName = dialog.FileName;
                var columns = GetExportColumns().ToList();
                await Task.Run(() => WriteXlsx(fileName, records, columns));
                MessageBox.Show($"Đã xuất {records.Count} hồ sơ:\n{dialog.FileName}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Export", "ExportRecordsFromRecordList", ex, $"Failed to export records to {dialog.FileName}.");
                MessageBox.Show($"Không thể xuất dữ liệu. Vui lòng thử lại.\n\nChi tiết: {ex.Message}", "Lỗi xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetExporting(false);
            }


        }
        private void SetExporting(bool value)
        {
            _isExporting = value;
            (ExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private static IEnumerable<ExportColumn> GetExportColumns()
        {
            yield return new ExportColumn("STT", record => record.Index.ToString(CultureInfo.InvariantCulture), ExportColumnType.Number);
            yield return new ExportColumn("Mã hồ sơ", record => record.RecordCode);
            yield return new ExportColumn("Ngày tiếp nhận", record => record.ReceivedDate);
            yield return new ExportColumn("Người gửi đơn", record => record.SenderName);
            yield return new ExportColumn("Địa bàn", record => record.AreaName);
            yield return new ExportColumn("Loại vụ việc", record => record.CaseType);
            yield return new ExportColumn("Lĩnh vực", record => record.Field);
            yield return new ExportColumn("Trạng thái", record => record.Status);
        }

        private static void WriteXlsx(string filePath, IReadOnlyList<ExportRecordPreview> records, IReadOnlyList<ExportColumn> columns)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
            AddZipText(archive, "[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
</Types>");
            AddZipText(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
            AddZipText(archive, "xl/workbook.xml", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Ho so"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");
            AddZipText(archive, "xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
</Relationships>");
            AddZipText(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(records, columns));
        }

        private static string BuildWorksheetXml(IReadOnlyList<ExportRecordPreview> records, IReadOnlyList<ExportColumn> columns)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };
            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                writer.WriteStartElement("sheetData");

                WriteXlsxRow(writer, 1, columns.Select(column => XlsxCellValue.Text(column.Header)));
                var rowIndex = 2;
                foreach (var record in records)
                {
                    WriteXlsxRow(writer, rowIndex++, columns.Select(column => XlsxCellValue.FromColumn(column, record)));
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteXlsxRow(XmlWriter writer, int rowIndex, IEnumerable<XlsxCellValue> values)
        {
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));

            foreach (var cell in values)
            {
                writer.WriteStartElement("c");
                if (cell.Type == ExportColumnType.Number)
                {
                    writer.WriteElementString("v", cell.Value ?? "0");
                }
                else
                {
                    writer.WriteAttributeString("t", "inlineStr");
                    writer.WriteStartElement("is");
                    writer.WriteElementString("t", cell.Value ?? string.Empty);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void AddZipText(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(content);
        }

        private void ViewRecord(string recordCode)
        {
            SelectedRecordDetail = _dataService.GetRecordForm(recordCode);
        }

        private void CloseDetail()
        {
            SelectedRecordDetail = null;
        }

        private void EditRecord(string recordCode)
        {
            _editRecord(recordCode);
        }

        private void ClassifyRecord(string recordCode)
        {
            _classifyRecord(recordCode);
        }

        private void DeleteRecord(string recordCode)
        {
            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa hồ sơ {recordCode}?",
                "Xác nhận xóa hồ sơ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                if (_dataService.DeleteRecord(recordCode))
                {
                    AppLogger.Info("Records", "DeleteRecordFromList", "Record deleted.", recordCode);
                    MessageBox.Show("Đã xóa hồ sơ khỏi cơ sở dữ liệu.", "Xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
                    Reload();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Records", "DeleteRecordFromList", ex, "Failed to delete record from list.", recordCode);
                MessageBox.Show($"Không thể xóa hồ sơ.\n\nChi tiết: {ex.Message}", "Lỗi xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RaisePageCommandStates()
        {
            _previousPageCommand.RaiseCanExecuteChanged();
            _nextPageCommand.RaiseCanExecuteChanged();
        }

        private static string GetFirstOrDefault(ObservableCollection<string> items)
        {
            return items.Count > 0 ? items[0] : null;
        }

        private static string GetFirstOrDefault(ObservableCollection<AreaSelectionOption> items)
        {
            return items.Count > 0 ? items[0].FilterValue : null;
        }

        private static void ReplaceAreaOptions(ObservableCollection<AreaSelectionOption> target, ObservableCollection<AreaSelectionOption> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private bool FilterArea(object item)
        {
            if (!(item is AreaSelectionOption area))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(AreaSearchText))
            {
                return true;
            }

            var search = NormalizeText(AreaSearchText);
            return NormalizeText(area.DisplayName).Contains(search)
                || NormalizeText(area.GroupName).Contains(search)
                || area.Children.Any(child => NormalizeText(child.DisplayName).Contains(search));
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.ToLower(CultureInfo.CurrentCulture).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString()
                .Replace("Ä‘", "d")
                .Replace("Ä", "d")
                .Normalize(NormalizationForm.FormC);
        }

        private sealed class ExportColumn
        {
            public ExportColumn(string header, Func<ExportRecordPreview, string> getValue, ExportColumnType type = ExportColumnType.Text)
            {
                Header = header;
                GetValue = getValue;
                Type = type;
            }

            public string Header { get; }
            public Func<ExportRecordPreview, string> GetValue { get; }
            public ExportColumnType Type { get; }
        }

        private sealed class XlsxCellValue
        {
            private XlsxCellValue(string value, ExportColumnType type)
            {
                Value = value;
                Type = type;
            }

            public string Value { get; }
            public ExportColumnType Type { get; }

            public static XlsxCellValue Text(string value)
            {
                return new XlsxCellValue(value, ExportColumnType.Text);
            }

            public static XlsxCellValue FromColumn(ExportColumn column, ExportRecordPreview record)
            {
                return new XlsxCellValue(column.GetValue(record), column.Type);
            }
        }

        private enum ExportColumnType
        {
            Text,
            Number
        }
    }
}
