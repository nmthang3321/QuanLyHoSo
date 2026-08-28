using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class ExportViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private DateTime? _fromDate;
        private string _resultRangeText;
        private string _searchText;
        private string _selectedArea;
        private string _selectedCaseType;
        private string _selectedField;
        private string _selectedProcessor;
        private string _selectedSortOption;
        private string _selectedStatus;
        private bool _includeHeaderRow = true;
        private bool _isCsvFormat;
        private bool _isExcelFormat = true;
        private bool _showAreaColumn = true;
        private bool _showCaseTypeColumn = true;
        private bool _showFieldColumn = true;
        private bool _showIndexColumn = true;
        private bool _showReceivedDateColumn = true;
        private bool _showRecordCodeColumn = true;
        private bool _showSenderNameColumn = true;
        private bool _showStatusColumn = true;
        private DateTime? _toDate;
        private string _totalRecordsText;

        public ExportViewModel()
        {
            _dataService = AppDataService.Instance;

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
            Areas = new ObservableCollection<string>(_dataService.GetAreaNames(includeAll: true));
            Processors = new ObservableCollection<string>(_dataService.GetProcessorNames(includeAll: true));
            SortOptions = new ObservableCollection<string> { "Ngày tiếp nhận mới nhất trước", "Ngày tiếp nhận cũ nhất trước", "Trạng thái", "Địa bàn" };
            PreviewRecords = new ObservableCollection<ExportRecordPreview>();
            ApplyFilterCommand = new RelayCommand(ApplyFilters);
            ResetFilterCommand = new RelayCommand(ResetFilters);
            ExportCommand = new RelayCommand(ExportData);
            ResetFilters();
        }

        public ObservableCollection<string> Statuses { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> Areas { get; }
        public ObservableCollection<string> Processors { get; }
        public ObservableCollection<string> SortOptions { get; }
        public ObservableCollection<ExportRecordPreview> PreviewRecords { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand ExportCommand { get; }

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
            set => SetProperty(ref _selectedArea, value);
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

        public string TotalRecordsText
        {
            get => _totalRecordsText;
            private set => SetProperty(ref _totalRecordsText, value);
        }

        public string ResultRangeText
        {
            get => _resultRangeText;
            private set => SetProperty(ref _resultRangeText, value);
        }

        public bool IsExcelFormat
        {
            get => _isExcelFormat;
            set
            {
                if (SetProperty(ref _isExcelFormat, value) && value)
                {
                    IsCsvFormat = false;
                }
            }
        }

        public bool IsCsvFormat
        {
            get => _isCsvFormat;
            set
            {
                if (SetProperty(ref _isCsvFormat, value) && value)
                {
                    IsExcelFormat = false;
                }
            }
        }

        public bool IncludeHeaderRow
        {
            get => _includeHeaderRow;
            set => SetProperty(ref _includeHeaderRow, value);
        }

        public bool ShowIndexColumn
        {
            get => _showIndexColumn;
            set => SetProperty(ref _showIndexColumn, value);
        }

        public bool ShowRecordCodeColumn
        {
            get => _showRecordCodeColumn;
            set => SetProperty(ref _showRecordCodeColumn, value);
        }

        public bool ShowReceivedDateColumn
        {
            get => _showReceivedDateColumn;
            set => SetProperty(ref _showReceivedDateColumn, value);
        }

        public bool ShowSenderNameColumn
        {
            get => _showSenderNameColumn;
            set => SetProperty(ref _showSenderNameColumn, value);
        }

        public bool ShowAreaColumn
        {
            get => _showAreaColumn;
            set => SetProperty(ref _showAreaColumn, value);
        }

        public bool ShowCaseTypeColumn
        {
            get => _showCaseTypeColumn;
            set => SetProperty(ref _showCaseTypeColumn, value);
        }

        public bool ShowFieldColumn
        {
            get => _showFieldColumn;
            set => SetProperty(ref _showFieldColumn, value);
        }

        public bool ShowStatusColumn
        {
            get => _showStatusColumn;
            set => SetProperty(ref _showStatusColumn, value);
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
            SelectedProcessor = GetFirstOrDefault(Processors);
            SearchText = string.Empty;
            SelectedSortOption = GetFirstOrDefault(SortOptions);
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var records = _dataService.GetExportPreview(
                FromDate,
                ToDate,
                SelectedStatus,
                SelectedCaseType,
                SelectedField,
                SelectedArea,
                SelectedProcessor,
                SearchText,
                SelectedSortOption);
            PreviewRecords.Clear();
            foreach (var record in records)
            {
                PreviewRecords.Add(record);
            }

            var total = _dataService.CountExportRecords(
                FromDate,
                ToDate,
                SelectedStatus,
                SelectedCaseType,
                SelectedField,
                SelectedArea,
                SelectedProcessor,
                SearchText);
            TotalRecordsText = $"Tổng số hồ sơ: {total}";
            ResultRangeText = total == 0
                ? "Không có kết quả phù hợp"
                : $"Hiển thị 1 - {PreviewRecords.Count} của {total} kết quả";
        }

        private void ExportData()
        {
            ApplyFilters();

            var columns = GetSelectedColumns().ToList();
            if (columns.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một cột dữ liệu để xuất.", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var records = _dataService.GetExportPreview(
                FromDate,
                ToDate,
                SelectedStatus,
                SelectedCaseType,
                SelectedField,
                SelectedArea,
                SelectedProcessor,
                SearchText,
                SelectedSortOption,
                take: int.MaxValue);

            if (records.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu phù hợp với bộ lọc hiện tại.", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var extension = IsCsvFormat ? "csv" : "xlsx";
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = extension,
                FileName = $"QuanLyHoSo_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}",
                Filter = IsCsvFormat ? "CSV (*.csv)|*.csv" : "Excel Workbook (*.xlsx)|*.xlsx",
                OverwritePrompt = true,
                Title = "Chọn nơi lưu file xuất dữ liệu"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (IsCsvFormat)
                {
                    WriteCsv(dialog.FileName, records, columns);
                }
                else
                {
                    WriteXlsx(dialog.FileName, records, columns);
                }

                MessageBox.Show($"Đã xuất {records.Count} hồ sơ:\n{dialog.FileName}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xuất dữ liệu. Vui lòng thử lại.\n\nChi tiết: {ex.Message}", "Lỗi xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<ExportColumn> GetSelectedColumns()
        {
            if (ShowIndexColumn)
            {
                yield return new ExportColumn("STT", record => record.Index.ToString(CultureInfo.InvariantCulture), ExportColumnType.Number);
            }

            if (ShowRecordCodeColumn)
            {
                yield return new ExportColumn("Mã hồ sơ", record => record.RecordCode);
            }

            if (ShowReceivedDateColumn)
            {
                yield return new ExportColumn("Ngày tiếp nhận", record => record.ReceivedDate, ExportColumnType.DateText);
            }

            if (ShowSenderNameColumn)
            {
                yield return new ExportColumn("Người gửi đơn", record => record.SenderName);
            }

            if (ShowAreaColumn)
            {
                yield return new ExportColumn("Địa bàn", record => record.AreaName);
            }

            if (ShowCaseTypeColumn)
            {
                yield return new ExportColumn("Loại vụ việc", record => record.CaseType);
            }

            if (ShowFieldColumn)
            {
                yield return new ExportColumn("Lĩnh vực", record => record.Field);
            }

            if (ShowStatusColumn)
            {
                yield return new ExportColumn("Trạng thái", record => record.Status);
            }
        }

        private void WriteCsv(string filePath, IReadOnlyList<ExportRecordPreview> records, IReadOnlyList<ExportColumn> columns)
        {
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            if (IncludeHeaderRow)
            {
                writer.WriteLine(string.Join(",", columns.Select(column => EscapeCsv(column.Header))));
            }

            foreach (var record in records)
            {
                writer.WriteLine(string.Join(",", columns.Select(column => FormatCsvValue(column, record))));
            }
        }

        private static string FormatCsvValue(ExportColumn column, ExportRecordPreview record)
        {
            var value = column.GetValue(record);
            return column.Type == ExportColumnType.DateText
                ? $"=\"{(value ?? string.Empty).Replace("\"", "\"\"")}\""
                : EscapeCsv(value);
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            return value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n")
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        private void WriteXlsx(string filePath, IReadOnlyList<ExportRecordPreview> records, IReadOnlyList<ExportColumn> columns)
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

        private string BuildWorksheetXml(IReadOnlyList<ExportRecordPreview> records, IReadOnlyList<ExportColumn> columns)
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

                var rowIndex = 1;
                if (IncludeHeaderRow)
                {
                    WriteXlsxRow(writer, rowIndex++, columns.Select(column => XlsxCellValue.Text(column.Header)));
                }

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

        private static string GetFirstOrDefault(ObservableCollection<string> items)
        {
            return items.Count > 0 ? items[0] : null;
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
            Number,
            DateText
        }
    }
}
