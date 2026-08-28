using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordInputViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private string _editingRecordCode;

        public RecordInputViewModel(Action showRecordList)
        {
            _dataService = AppDataService.Instance;

            ReceiveSources = new ObservableCollection<string>(_dataService.GetCatalogValues("ReceiveSource"));
            Areas = new ObservableCollection<string>(_dataService.GetAreaNames());
            FilteredAreas = CollectionViewSource.GetDefaultView(Areas);
            FilteredAreas.Filter = FilterArea;
            CaseTypes = new ObservableCollection<string>(_dataService.GetCatalogValues("CaseType"));
            Fields = new ObservableCollection<string>(_dataService.GetCatalogValues("Field"));
            ContentGroups = new ObservableCollection<string>(_dataService.GetCatalogValues("ContentGroup"));
            Priorities = new ObservableCollection<string>(_dataService.GetCatalogValues("Priority"));
            HandlingMethods = new ObservableCollection<string>(_dataService.GetCatalogValues("ExpectedHandlingMethod"));
            Attachments = new ObservableCollection<AttachmentDraft>();
            Attachments.CollectionChanged += Attachments_CollectionChanged;
            ShowRecordListCommand = new RelayCommand(showRecordList ?? (() => { }));
            NewCommand = new RelayCommand(ClearForm);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(ClearForm);
            DeleteCommand = new RelayCommand(DeleteCurrentRecord);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            OpenAttachmentCommand = new RelayCommand(OpenAttachment);

            ClearForm();
        }

        public ObservableCollection<string> ReceiveSources { get; }
        public ObservableCollection<string> Areas { get; }
        public ICollectionView FilteredAreas { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> ContentGroups { get; }
        public ObservableCollection<string> Priorities { get; }
        public ObservableCollection<string> HandlingMethods { get; }
        public ObservableCollection<AttachmentDraft> Attachments { get; }
        public bool HasAttachments => Attachments.Count > 0;
        public ICommand ShowRecordListCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RemoveAttachmentCommand { get; }
        public ICommand OpenAttachmentCommand { get; }

        public string RecordCode { get; set; }
        public string ReceiveSource { get; set; }
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string ContactAddress { get; set; }
        public string IncidentAddress { get; set; }
        public string Content { get; set; }
        public string CaseType { get; set; }
        public string ContentGroup { get; set; }
        public string Field { get; set; }
        public string RelatedPerson { get; set; }
        public string ExpectedHandlingMethod { get; set; }
        public string SeverityLevel { get; set; }
        public string ExpectedResultDate { get; set; }
        public string PriorityLevel { get; set; }
        public string Note { get; set; }
        public string AdditionalNote { get; set; }

        public void LoadRecord(string recordCode)
        {
            LoadRecord(_dataService.GetRecordForm(recordCode));
        }

        private void LoadRecord(RecordFormDraft record)
        {
            _editingRecordCode = record.RecordCode;
            RecordCode = record.RecordCode;
            ReceivedDate = record.ReceivedDate;
            SelectedReceivedDate = ParseDisplayDate(record.ReceivedDate);
            ReceiveSource = record.ReceiveSource;
            ReceiverName = record.ReceiverName;
            SenderName = record.SenderName;
            SenderPhone = record.SenderPhone;
            ContactAddress = record.ContactAddress;
            AreaName = record.AreaName;
            AreaSearchText = record.AreaName;
            IncidentAddress = record.IncidentAddress;
            Content = record.Content;
            CaseType = record.CaseType;
            ContentGroup = record.ContentGroup;
            Field = record.Field;
            RelatedPerson = record.RelatedPerson;
            ExpectedHandlingMethod = record.ExpectedHandlingMethod;
            SeverityLevel = record.SeverityLevel;
            ExpectedResultDate = record.ExpectedResultDate;
            PriorityLevel = record.PriorityLevel;
            Note = record.Note;
            AdditionalNote = record.AdditionalNote;

            Attachments.Clear();
            foreach (var attachment in record.Attachments)
            {
                Attachments.Add(attachment);
            }

            RaiseFormPropertyChanges();
        }

        private void ClearForm()
        {
            _editingRecordCode = null;
            RecordCode = string.Empty;
            ReceivedDate = string.Empty;
            SelectedReceivedDate = null;
            ReceiveSource = null;
            ReceiverName = string.Empty;
            SenderName = string.Empty;
            SenderPhone = string.Empty;
            ContactAddress = string.Empty;
            AreaName = null;
            AreaSearchText = string.Empty;
            IncidentAddress = string.Empty;
            Content = string.Empty;
            CaseType = null;
            ContentGroup = null;
            Field = null;
            RelatedPerson = string.Empty;
            ExpectedHandlingMethod = null;
            SeverityLevel = null;
            ExpectedResultDate = string.Empty;
            PriorityLevel = null;
            Note = string.Empty;
            AdditionalNote = string.Empty;
            Attachments.Clear();
            RaiseFormPropertyChanges();
        }

        public void AddAttachmentFiles(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return;
            }

            var skippedFiles = new System.Collections.Generic.List<string>();
            foreach (var filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                {
                    continue;
                }

                var fileInfo = new System.IO.FileInfo(filePath);
                if (!IsSupportedAttachment(fileInfo))
                {
                    skippedFiles.Add(fileInfo.Name);
                    continue;
                }

                if (Attachments.Any(item => string.Equals(item.FileName, fileInfo.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                Attachments.Add(new AttachmentDraft
                {
                    FileName = fileInfo.Name,
                    FileSize = FormatFileSize(fileInfo.Length),
                    FilePath = fileInfo.FullName
                });
            }

            if (skippedFiles.Count > 0)
            {
                MessageBox.Show(
                    "Một số file không được thêm vì không đúng định dạng hoặc vượt quá 10MB/file:\n\n- " + string.Join("\n- ", skippedFiles),
                    "Tài liệu đính kèm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Save()
        {
            var validationMessage = ValidateRequiredFields();
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                MessageBox.Show(validationMessage, "Thiếu thông tin bắt buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _dataService.SaveRecordForm(BuildDraft(), _editingRecordCode);
                _editingRecordCode = RecordCode?.Trim();
                MessageBox.Show("Đã lưu hồ sơ vào cơ sở dữ liệu.", "Lưu hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Records", "SaveRecordForm", ex, "Failed to save record form.", RecordCode);
                MessageBox.Show($"Không thể lưu hồ sơ. Vui lòng kiểm tra lại dữ liệu.\n\nChi tiết: {ex.Message}", "Lỗi lưu hồ sơ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCurrentRecord()
        {
            var recordCode = string.IsNullOrWhiteSpace(_editingRecordCode) ? RecordCode : _editingRecordCode;
            if (string.IsNullOrWhiteSpace(recordCode))
            {
                MessageBox.Show("Chưa có hồ sơ để xóa.", "Xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                    AppLogger.Info("Records", "DeleteRecord", "Record deleted.", recordCode);
                    MessageBox.Show("Đã xóa hồ sơ khỏi cơ sở dữ liệu.", "Xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    return;
                }

                MessageBox.Show("Không tìm thấy hồ sơ trong cơ sở dữ liệu.", "Xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Records", "DeleteRecord", ex, "Failed to delete record.", recordCode);
                MessageBox.Show($"Không thể xóa hồ sơ.\n\nChi tiết: {ex.Message}", "Lỗi xóa hồ sơ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RecordFormDraft BuildDraft()
        {
            return new RecordFormDraft
            {
                RecordCode = RecordCode,
                ReceivedDate = ReceivedDate,
                ReceiveSource = ReceiveSource,
                ReceiverName = ReceiverName,
                SenderName = SenderName,
                SenderPhone = SenderPhone,
                ContactAddress = ContactAddress,
                AreaName = AreaName,
                IncidentAddress = IncidentAddress,
                Content = Content,
                CaseType = CaseType,
                ContentGroup = ContentGroup,
                Field = Field,
                RelatedPerson = RelatedPerson,
                ExpectedHandlingMethod = ExpectedHandlingMethod,
                SeverityLevel = SeverityLevel,
                ExpectedResultDate = ExpectedResultDate,
                PriorityLevel = PriorityLevel,
                Note = Note,
                AdditionalNote = AdditionalNote,
                Attachments = Attachments.ToList()
            };
        }

        private void RemoveAttachment(object parameter)
        {
            if (parameter is AttachmentDraft attachment)
            {
                Attachments.Remove(attachment);
            }
        }

        private void OpenAttachment(object parameter)
        {
            if (parameter is not AttachmentDraft attachment || string.IsNullOrWhiteSpace(attachment.FilePath))
            {
                MessageBox.Show("Tài liệu này chưa có đường dẫn file để mở.", "Xem tài liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!System.IO.File.Exists(attachment.FilePath))
            {
                MessageBox.Show("Không tìm thấy file trên máy. Vui lòng chọn lại tài liệu.", "Xem tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = attachment.FilePath,
                UseShellExecute = true
            });
        }

        private string ValidateRequiredFields()
        {
            var missingFields = new[]
            {
                (Name: "Số hồ sơ / Số đơn", IsMissing: string.IsNullOrWhiteSpace(RecordCode)),
                (Name: "Ngày tiếp nhận", IsMissing: !SelectedReceivedDate.HasValue),
                (Name: "Nguồn tiếp nhận", IsMissing: string.IsNullOrWhiteSpace(ReceiveSource)),
                (Name: "Người tiếp nhận", IsMissing: string.IsNullOrWhiteSpace(ReceiverName)),
                (Name: "Người gửi đơn / Người tố giác", IsMissing: string.IsNullOrWhiteSpace(SenderName)),
                (Name: "Địa bàn (xã/phường/đặc khu)", IsMissing: string.IsNullOrWhiteSpace(AreaName)),
                (Name: "Nội dung đơn / Nội dung vụ việc", IsMissing: string.IsNullOrWhiteSpace(Content)),
                (Name: "Loại vụ việc", IsMissing: string.IsNullOrWhiteSpace(CaseType)),
                (Name: "Nhóm nội dung", IsMissing: string.IsNullOrWhiteSpace(ContentGroup)),
                (Name: "Lĩnh vực", IsMissing: string.IsNullOrWhiteSpace(Field))
            }
            .Where(field => field.IsMissing)
            .Select(field => field.Name)
            .ToList();

            if (missingFields.Count == 0)
            {
                return string.Empty;
            }

            return "Vui lòng nhập đầy đủ các thông tin bắt buộc:\n\n- " + string.Join("\n- ", missingFields);
        }

        private void RaiseFormPropertyChanges()
        {
            OnPropertyChanged(nameof(RecordCode));
            OnPropertyChanged(nameof(ReceivedDate));
            OnPropertyChanged(nameof(SelectedReceivedDate));
            OnPropertyChanged(nameof(ReceiveSource));
            OnPropertyChanged(nameof(ReceiverName));
            OnPropertyChanged(nameof(SenderName));
            OnPropertyChanged(nameof(SenderPhone));
            OnPropertyChanged(nameof(ContactAddress));
            OnPropertyChanged(nameof(AreaName));
            OnPropertyChanged(nameof(AreaSearchText));
            OnPropertyChanged(nameof(IncidentAddress));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(CaseType));
            OnPropertyChanged(nameof(ContentGroup));
            OnPropertyChanged(nameof(Field));
            OnPropertyChanged(nameof(RelatedPerson));
            OnPropertyChanged(nameof(ExpectedHandlingMethod));
            OnPropertyChanged(nameof(SeverityLevel));
            OnPropertyChanged(nameof(ExpectedResultDate));
            OnPropertyChanged(nameof(PriorityLevel));
            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(AdditionalNote));
            OnPropertyChanged(nameof(HasAttachments));
        }

        private string _receivedDate;
        public string ReceivedDate
        {
            get => _receivedDate;
            set => SetProperty(ref _receivedDate, value);
        }

        private DateTime? _selectedReceivedDate;
        public DateTime? SelectedReceivedDate
        {
            get => _selectedReceivedDate;
            set
            {
                if (SetProperty(ref _selectedReceivedDate, value))
                {
                    ReceivedDate = value.HasValue
                        ? value.Value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
                        : string.Empty;
                }
            }
        }

        private string _areaName;
        public string AreaName
        {
            get => _areaName;
            set
            {
                if (SetProperty(ref _areaName, value) && !string.Equals(_areaSearchText, value, System.StringComparison.Ordinal))
                {
                    AreaSearchText = value;
                }
            }
        }

        private string _areaSearchText;
        public string AreaSearchText
        {
            get => _areaSearchText;
            set
            {
                if (!SetProperty(ref _areaSearchText, value))
                {
                    return;
                }

                FilteredAreas.Refresh();

                var exactMatch = Areas.FirstOrDefault(area => string.Equals(area, value, System.StringComparison.CurrentCultureIgnoreCase));
                if (exactMatch != null && !string.Equals(_areaName, exactMatch, System.StringComparison.Ordinal))
                {
                    _areaName = exactMatch;
                    OnPropertyChanged(nameof(AreaName));
                }
            }
        }

        private bool FilterArea(object item)
        {
            if (!(item is string area))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(AreaSearchText))
            {
                return true;
            }

            return NormalizeText(area).Contains(NormalizeText(AreaSearchText));
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
                .Replace('đ', 'd')
                .Replace('Đ', 'd')
                .Normalize(NormalizationForm.FormC);
        }

        private static DateTime? ParseDisplayDate(string value)
        {
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var exactDate))
            {
                return exactDate;
            }

            return DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                ? date
                : (DateTime?)null;
        }

        private static bool IsSupportedAttachment(System.IO.FileInfo fileInfo)
        {
            const long maxFileSize = 10 * 1024 * 1024;
            var extension = fileInfo.Extension?.ToLowerInvariant();
            return fileInfo.Length <= maxFileSize
                && (extension == ".pdf" || extension == ".jpg" || extension == ".jpeg" || extension == ".png");
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / 1024d / 1024d:0.#} MB";
            }

            return $"{Math.Max(1, bytes / 1024)} KB";
        }

        private void Attachments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasAttachments));
        }
    }
}
