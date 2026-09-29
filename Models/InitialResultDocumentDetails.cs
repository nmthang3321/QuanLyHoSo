using System;

namespace QuanLyHoSo.Models
{
    public sealed class InitialResultDocumentDetails
    {
        public string TransferNumber { get; set; }
        public DateTime? TransferDate { get; set; }
        public DateTime? ComplaintDate { get; set; }
        public string Review { get; set; }
        public string Proposal { get; set; }
        public string CommanderApproverName { get; set; }
        public string ProposingOfficerName { get; set; }
        public string LeaderApproverName { get; set; }

        public string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(TransferNumber)) return "Vui lòng nhập phiếu chuyển đơn số.";
            if (!TransferDate.HasValue) return "Vui lòng chọn ngày phiếu chuyển đơn.";
            if (!ComplaintDate.HasValue) return "Vui lòng chọn ngày đề đơn tố cáo.";
            if (string.IsNullOrWhiteSpace(Review)) return "Vui lòng nhập nhận xét.";
            if (string.IsNullOrWhiteSpace(Proposal)) return "Vui lòng nhập đề xuất.";
            if (string.IsNullOrWhiteSpace(CommanderApproverName)) return "Vui lòng nhập tên đội trưởng.";
            if (string.IsNullOrWhiteSpace(ProposingOfficerName)) return "Vui lòng chọn người xử lý để điền cán bộ đề xuất.";
            if (string.IsNullOrWhiteSpace(LeaderApproverName)) return "Vui lòng nhập tên lãnh đạo duyệt.";
            return string.Empty;
        }
    }
}
