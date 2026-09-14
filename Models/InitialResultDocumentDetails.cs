using System;

namespace QuanLyHoSo.Models
{
    public sealed class InitialResultDocumentDetails
    {
        public string TransferNumber { get; set; }
        public DateTime? TransferDate { get; set; }
        public DateTime? ComplaintDate { get; set; }

        public string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(TransferNumber)) return "Vui lòng nhập phiếu chuyển đơn số.";
            if (!TransferDate.HasValue) return "Vui lòng chọn ngày phiếu chuyển đơn.";
            if (!ComplaintDate.HasValue) return "Vui lòng chọn ngày đề đơn tố cáo.";
            return string.Empty;
        }
    }
}
