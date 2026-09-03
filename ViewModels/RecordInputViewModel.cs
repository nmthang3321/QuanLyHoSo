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
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Infrastructure.Logging;
using QuanLyHoSo.Infrastructure.Security;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordInputViewModel : ViewModelBase
    {
        private readonly AppDataService _dataService;
        private readonly Action _goBack;
        private string _editingRecordCode;
        private RecordFormDraft _originalDraft;

        public RecordInputViewModel(Action goBack = null)
        {
            _dataService = AppDataService.Instance;
            _goBack = goBack ?? (() => { });

            ReceiveSources = new ObservableCollection<string>(_dataService.GetCatalogValues("ReceiveSource"));
            ReceiverNames = new ObservableCollection<string>(_dataService.GetProcessorNames());
            Areas = AreaSelectionOptions.Build(_dataService.GetAreaNames(), includeGroupRows: true, groupRowsSelectable: false);
            FilteredAreas = AreaSelectionOptions.Filter(Areas, null);
            CaseTypes = new ObservableCollection<string>(_dataService.GetCatalogValues("CaseType"));
            Fields = new ObservableCollection<string>(_dataService.GetCatalogValues("Field"));
            ContentGroups = new ObservableCollection<string>(_dataService.GetCatalogValues("ContentGroup"));
            Priorities = new ObservableCollection<string>(_dataService.GetCatalogValues("Priority"));
            HandlingMethods = new ObservableCollection<string>(_dataService.GetCatalogValues("ExpectedHandlingMethod"));
            _dataService.CatalogChanged += DataService_CatalogChanged;
            Attachments = new ObservableCollection<AttachmentDraft>();
            Attachments.CollectionChanged += Attachments_CollectionChanged;
            NewCommand = new RelayCommand(ClearForm);
            SaveCommand = new RelayCommand(Save, () => CanWrite);
            BackCommand = new RelayCommand(_goBack);
            CancelCommand = new RelayCommand(CancelForm);
            DeleteCommand = new RelayCommand(DeleteCurrentRecord, () => CanWrite);
            RemoveAttachmentCommand = new RelayCommand(RemoveAttachment);
            OpenAttachmentCommand = new RelayCommand(OpenAttachment);

            ClearForm();
        }

        public ObservableCollection<string> ReceiveSources { get; }
        public ObservableCollection<string> ReceiverNames { get; }
        public ObservableCollection<AreaSelectionOption> Areas { get; }
        public ObservableCollection<AreaSelectionOption> FilteredAreas { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> ContentGroups { get; }
        public ObservableCollection<string> Priorities { get; }
        public ObservableCollection<string> HandlingMethods { get; }
        public ObservableCollection<AttachmentDraft> Attachments { get; }
        public bool HasAttachments => Attachments.Count > 0;
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RemoveAttachmentCommand { get; }
        public ICommand OpenAttachmentCommand { get; }
        public bool CanWrite => AuthContext.CanWrite && !AppPathSettings.Current.IsClientMode;
        public bool IsEditingExistingRecord => !string.IsNullOrWhiteSpace(_editingRecordCode);
        public string SaveButtonText => IsEditingExistingRecord ? "Cập nhật" : "Lưu";

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
        public string SenderExpectedHandlingMethod { get; set; }
        public string SeverityLevel { get; set; }
        public string ExpectedResultDate { get; set; }
        public string Note { get; set; }
        public string AdditionalNote { get; set; }

        public void LoadRecord(string recordCode)
        {
            LoadRecord(_dataService.GetRecordForm(recordCode));
        }

        public void PrepareNewRecord()
        {
            ClearForm();
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
            SenderExpectedHandlingMethod = record.SenderExpectedHandlingMethod;
            SeverityLevel = record.SeverityLevel;
            ExpectedResultDate = record.ExpectedResultDate;
            SelectedExpectedResultDate = ParseDisplayDate(record.ExpectedResultDate);
            Note = record.Note;
            AdditionalNote = record.AdditionalNote;

            Attachments.Clear();
            foreach (var attachment in record.Attachments)
            {
                Attachments.Add(attachment);
            }

            _originalDraft = BuildDraft();

            OnPropertyChanged(nameof(IsEditingExistingRecord));
            OnPropertyChanged(nameof(SaveButtonText));
            RaiseFormPropertyChanges();
        }

        private void ClearForm()
        {
            _editingRecordCode = null;
            _originalDraft = null;
            RecordCode = _dataService.GetNextRecordCode();
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
            SenderExpectedHandlingMethod = null;
            SeverityLevel = null;
            ExpectedResultDate = string.Empty;
            SelectedExpectedResultDate = null;
            Note = string.Empty;
            AdditionalNote = string.Empty;
            Attachments.Clear();
            OnPropertyChanged(nameof(IsEditingExistingRecord));
            OnPropertyChanged(nameof(SaveButtonText));
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

        private void CancelForm()
        {
            if (!HasUnsavedDraft())
            {
                ClearForm();
                return;
            }

            var result = MessageBox.Show(
                "Bạn có chắc muốn hủy bỏ dữ liệu chưa lưu? Tất cả thông tin đang nhập sẽ bị xóa.",
                "Xác nhận hủy bỏ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ClearForm();
            }
        }

        private void Save()
        {
            if (!CanWrite)
            {
                MessageBox.Show("Tài khoản hiện tại chỉ được xem dữ liệu.", "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var validationMessage = ValidateRequiredFields();
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                MessageBox.Show(validationMessage, "Thiếu thông tin bắt buộc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var draft = BuildDraft();
                var isEditing = IsEditingExistingRecord;
                if (!isEditing && !ConfirmSaveWhenSimilarRecordExists(draft))
                {
                    return;
                }

                var savedRecordCode = _dataService.SaveRecordForm(draft, _editingRecordCode);
                RecordCode = savedRecordCode;
                _editingRecordCode = savedRecordCode;
                _originalDraft = BuildDraft();
                OnPropertyChanged(nameof(RecordCode));
                OnPropertyChanged(nameof(IsEditingExistingRecord));
                OnPropertyChanged(nameof(SaveButtonText));
                var actionText = isEditing ? "cập nhật" : "lưu";
                MessageBox.Show($"Đã {actionText} hồ sơ vào cơ sở dữ liệu.", "Hồ sơ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Records", "SaveRecordForm", ex, "Failed to save record form.", RecordCode);
                MessageBox.Show($"Không thể lưu hồ sơ. Vui lòng kiểm tra lại dữ liệu.\n\nChi tiết: {ex.Message}", "Lỗi lưu hồ sơ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ConfirmSaveWhenSimilarRecordExists(RecordFormDraft draft)
        {
            var similarRecord = _dataService.FindSimilarRecord(draft);
            if (similarRecord == null)
            {
                return true;
            }

            var message =
                "Hệ thống phát hiện hồ sơ có thể trùng với hồ sơ đã có.\n\n" +
                $"Mã hồ sơ: {similarRecord.RecordCode}\n" +
                $"Ngày tiếp nhận: {similarRecord.ReceivedDate}\n" +
                $"Người gửi: {similarRecord.SenderName}\n" +
                $"Số điện thoại: {similarRecord.SenderPhone}\n" +
                $"Địa bàn: {similarRecord.AreaName}\n" +
                $"Loại vụ việc: {similarRecord.CaseType}\n" +
                $"Trạng thái: {similarRecord.Status}\n\n" +
                "Chọn Yes để vẫn tạo hồ sơ mới.\n" +
                "Chọn No để mở hồ sơ đã có.\n" +
                "Chọn Cancel để hủy lưu.";

            var result = MessageBox.Show(
                message,
                "Cảnh báo hồ sơ nghi trùng",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                return true;
            }

            if (result == MessageBoxResult.No)
            {
                LoadRecord(similarRecord.RecordCode);
            }

            return false;
        }

        private void DeleteCurrentRecord()
        {
            if (!CanWrite)
            {
                MessageBox.Show("Tài khoản hiện tại chỉ được xem dữ liệu.", "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                SenderExpectedHandlingMethod = SenderExpectedHandlingMethod,
                SeverityLevel = SeverityLevel,
                ExpectedResultDate = ExpectedResultDate,
                Note = Note,
                AdditionalNote = AdditionalNote,
                Attachments = Attachments.ToList()
            };
        }

        private bool HasUnsavedDraft()
        {
            return SelectedReceivedDate.HasValue
                || SelectedExpectedResultDate.HasValue
                || !string.IsNullOrWhiteSpace(ReceiveSource)
                || !string.IsNullOrWhiteSpace(ReceiverName)
                || !string.IsNullOrWhiteSpace(SenderName)
                || !string.IsNullOrWhiteSpace(SenderPhone)
                || !string.IsNullOrWhiteSpace(ContactAddress)
                || !string.IsNullOrWhiteSpace(AreaName)
                || !string.IsNullOrWhiteSpace(IncidentAddress)
                || !string.IsNullOrWhiteSpace(Content)
                || !string.IsNullOrWhiteSpace(CaseType)
                || !string.IsNullOrWhiteSpace(ContentGroup)
                || !string.IsNullOrWhiteSpace(Field)
                || !string.IsNullOrWhiteSpace(RelatedPerson)
                || !string.IsNullOrWhiteSpace(ExpectedHandlingMethod)
                || !string.IsNullOrWhiteSpace(SenderExpectedHandlingMethod)
                || !string.IsNullOrWhiteSpace(SeverityLevel)
                || !string.IsNullOrWhiteSpace(ExpectedResultDate)
                || !string.IsNullOrWhiteSpace(Note)
                || !string.IsNullOrWhiteSpace(AdditionalNote)
                || Attachments.Count > 0;
        }

        public bool ConfirmLeaveWithoutSaving()
        {
            var hasUnsavedChanges = IsEditingExistingRecord
                ? !AreDraftsEqual(_originalDraft, BuildDraft())
                : HasUnsavedDraft();

            if (!hasUnsavedChanges)
            {
                return true;
            }

            var result = MessageBox.Show(
                IsEditingExistingRecord
                    ? "Hồ sơ đang chỉnh sửa chưa được cập nhật. Nếu rời trang, dữ liệu thay đổi sẽ bị mất."
                    : "Dữ liệu hồ sơ chưa được lưu. Nếu rời trang, dữ liệu đã nhập sẽ bị mất.",
                "Xác nhận rời trang",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        private static bool AreDraftsEqual(RecordFormDraft left, RecordFormDraft right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.RecordCode, right.RecordCode, StringComparison.Ordinal)
                && string.Equals(left.ReceivedDate, right.ReceivedDate, StringComparison.Ordinal)
                && string.Equals(left.ReceiveSource, right.ReceiveSource, StringComparison.Ordinal)
                && string.Equals(left.ReceiverName, right.ReceiverName, StringComparison.Ordinal)
                && string.Equals(left.SenderName, right.SenderName, StringComparison.Ordinal)
                && string.Equals(left.SenderPhone, right.SenderPhone, StringComparison.Ordinal)
                && string.Equals(left.ContactAddress, right.ContactAddress, StringComparison.Ordinal)
                && string.Equals(left.AreaName, right.AreaName, StringComparison.Ordinal)
                && string.Equals(left.IncidentAddress, right.IncidentAddress, StringComparison.Ordinal)
                && string.Equals(left.Content, right.Content, StringComparison.Ordinal)
                && string.Equals(left.CaseType, right.CaseType, StringComparison.Ordinal)
                && string.Equals(left.ContentGroup, right.ContentGroup, StringComparison.Ordinal)
                && string.Equals(left.Field, right.Field, StringComparison.Ordinal)
                && string.Equals(left.RelatedPerson, right.RelatedPerson, StringComparison.Ordinal)
                && string.Equals(left.ExpectedHandlingMethod, right.ExpectedHandlingMethod, StringComparison.Ordinal)
                && string.Equals(left.SenderExpectedHandlingMethod, right.SenderExpectedHandlingMethod, StringComparison.Ordinal)
                && string.Equals(left.SeverityLevel, right.SeverityLevel, StringComparison.Ordinal)
                && string.Equals(left.ExpectedResultDate, right.ExpectedResultDate, StringComparison.Ordinal)
                && string.Equals(left.Note, right.Note, StringComparison.Ordinal)
                && string.Equals(left.AdditionalNote, right.AdditionalNote, StringComparison.Ordinal)
                && AreAttachmentsEqual(left.Attachments, right.Attachments);
        }

        private static bool AreAttachmentsEqual(
            System.Collections.Generic.IReadOnlyList<AttachmentDraft> left,
            System.Collections.Generic.IReadOnlyList<AttachmentDraft> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return left == right;
            }

            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index].FileName, right[index].FileName, StringComparison.Ordinal)
                    || !string.Equals(left[index].FileSize, right[index].FileSize, StringComparison.Ordinal)
                    || !string.Equals(left[index].FilePath, right[index].FilePath, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
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
            OnPropertyChanged(nameof(AreaDisplayName));
            OnPropertyChanged(nameof(IncidentAddress));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(CaseType));
            OnPropertyChanged(nameof(ContentGroup));
            OnPropertyChanged(nameof(Field));
            OnPropertyChanged(nameof(RelatedPerson));
            OnPropertyChanged(nameof(ExpectedHandlingMethod));
            OnPropertyChanged(nameof(SenderExpectedHandlingMethod));
            OnPropertyChanged(nameof(SeverityLevel));
            OnPropertyChanged(nameof(ExpectedResultDate));
            OnPropertyChanged(nameof(SelectedExpectedResultDate));
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

        private DateTime? _selectedExpectedResultDate;
        public DateTime? SelectedExpectedResultDate
        {
            get => _selectedExpectedResultDate;
            set
            {
                if (SetProperty(ref _selectedExpectedResultDate, value))
                {
                    ExpectedResultDate = value.HasValue
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
                if (!SetProperty(ref _areaName, value))
                {
                    return;
                }

                if (!string.Equals(_areaSearchText, value, System.StringComparison.Ordinal))
                {
                    AreaSearchText = value;
                }

                OnPropertyChanged(nameof(AreaDisplayName));
            }
        }

        public string AreaDisplayName => string.IsNullOrWhiteSpace(AreaName)
            ? "Chọn địa bàn"
            : AreaSelectionOptions.GetDisplayName(Areas, AreaName);

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

                ReplaceAreaOptions(FilteredAreas, AreaSelectionOptions.Filter(Areas, value));

                var exactMatch = AreaSelectionOptions.Flatten(Areas).FirstOrDefault(area => area.IsSelectable && string.Equals(area.DisplayName, value, System.StringComparison.CurrentCultureIgnoreCase));
                if (exactMatch != null && !string.Equals(_areaName, exactMatch.FilterValue, System.StringComparison.Ordinal))
                {
                    _areaName = exactMatch.FilterValue;
                    OnPropertyChanged(nameof(AreaName));
                    OnPropertyChanged(nameof(AreaDisplayName));
                }
            }
        }

        private static void ReplaceAreaOptions(ObservableCollection<AreaSelectionOption> target, ObservableCollection<AreaSelectionOption> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void DataService_CatalogChanged(string catalogType)
        {
            switch (catalogType)
            {
                case "ReceiveSource":
                    RefreshCatalogValues(ReceiveSources, catalogType, ReceiveSource, value => ReceiveSource = value, nameof(ReceiveSource));
                    break;
                case "ProcessorName":
                    RefreshListValues(ReceiverNames, _dataService.GetProcessorNames(), ReceiverName, value => ReceiverName = value, nameof(ReceiverName));
                    break;
                case "CaseType":
                    RefreshCatalogValues(CaseTypes, catalogType, CaseType, value => CaseType = value, nameof(CaseType));
                    break;
                case "Field":
                    RefreshCatalogValues(Fields, catalogType, Field, value => Field = value, nameof(Field));
                    break;
                case "ContentGroup":
                    RefreshCatalogValues(ContentGroups, catalogType, ContentGroup, value => ContentGroup = value, nameof(ContentGroup));
                    break;
                case "Priority":
                    RefreshCatalogValues(Priorities, catalogType, SeverityLevel, value => SeverityLevel = value, nameof(SeverityLevel));
                    if (!string.IsNullOrWhiteSpace(SeverityLevel) && !Priorities.Contains(SeverityLevel))
                    {
                        SeverityLevel = null;
                        OnPropertyChanged(nameof(SeverityLevel));
                    }
                    break;
                case "ExpectedHandlingMethod":
                    RefreshCatalogValues(HandlingMethods, catalogType, ExpectedHandlingMethod, value => ExpectedHandlingMethod = value, nameof(ExpectedHandlingMethod));
                    if (!string.IsNullOrWhiteSpace(SenderExpectedHandlingMethod) && !HandlingMethods.Contains(SenderExpectedHandlingMethod))
                    {
                        SenderExpectedHandlingMethod = null;
                        OnPropertyChanged(nameof(SenderExpectedHandlingMethod));
                    }
                    break;
            }
        }

        private void RefreshCatalogValues(ObservableCollection<string> target, string catalogType, string selectedValue, Action<string> setSelectedValue, string selectedPropertyName)
        {
            target.Clear();
            foreach (var item in _dataService.GetCatalogValues(catalogType))
            {
                target.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(selectedValue) && !target.Contains(selectedValue))
            {
                setSelectedValue(null);
                OnPropertyChanged(selectedPropertyName);
            }
        }

        private void RefreshListValues(ObservableCollection<string> target, System.Collections.Generic.IEnumerable<string> values, string selectedValue, Action<string> setSelectedValue, string selectedPropertyName)
        {
            target.Clear();
            foreach (var item in values)
            {
                target.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(selectedValue) && !target.Contains(selectedValue))
            {
                setSelectedValue(null);
                OnPropertyChanged(selectedPropertyName);
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

            return NormalizeText(area.DisplayName).Contains(NormalizeText(AreaSearchText))
                || NormalizeText(area.GroupName).Contains(NormalizeText(AreaSearchText))
                || area.Children.Any(child => NormalizeText(child.DisplayName).Contains(NormalizeText(AreaSearchText)));
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
