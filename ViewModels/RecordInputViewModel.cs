using System.Collections.ObjectModel;
using QuanLyHoSo.Infrastructure.Data;
using QuanLyHoSo.Models;

namespace QuanLyHoSo.ViewModels
{
    public sealed class RecordInputViewModel : ViewModelBase
    {
        public RecordInputViewModel()
        {
            var dataService = AppDataService.Instance;
            var record = dataService.GetLatestRecordForm();

            ReceiveSources = new ObservableCollection<string>(dataService.GetCatalogValues("ReceiveSource"));
            Areas = new ObservableCollection<string>(dataService.GetAreaNames());
            CaseTypes = new ObservableCollection<string>(dataService.GetCatalogValues("CaseType"));
            Fields = new ObservableCollection<string>(dataService.GetCatalogValues("Field"));
            ContentGroups = new ObservableCollection<string>(dataService.GetCatalogValues("ContentGroup"));
            Priorities = new ObservableCollection<string>(dataService.GetCatalogValues("Priority"));
            HandlingMethods = new ObservableCollection<string>(dataService.GetCatalogValues("ExpectedHandlingMethod"));
            Attachments = new ObservableCollection<AttachmentDraft>(record.Attachments);

            RecordCode = record.RecordCode;
            ReceivedDate = record.ReceivedDate;
            ReceiveSource = record.ReceiveSource;
            ReceiverName = record.ReceiverName;
            SenderName = record.SenderName;
            SenderPhone = record.SenderPhone;
            ContactAddress = record.ContactAddress;
            AreaName = record.AreaName;
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
        }

        public ObservableCollection<string> ReceiveSources { get; }
        public ObservableCollection<string> Areas { get; }
        public ObservableCollection<string> CaseTypes { get; }
        public ObservableCollection<string> Fields { get; }
        public ObservableCollection<string> ContentGroups { get; }
        public ObservableCollection<string> Priorities { get; }
        public ObservableCollection<string> HandlingMethods { get; }
        public ObservableCollection<AttachmentDraft> Attachments { get; }

        public string RecordCode { get; set; }
        public string ReceivedDate { get; set; }
        public string ReceiveSource { get; set; }
        public string ReceiverName { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string ContactAddress { get; set; }
        public string AreaName { get; set; }
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
    }
}
