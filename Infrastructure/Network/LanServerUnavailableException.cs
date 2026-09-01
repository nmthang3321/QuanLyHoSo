using System;

namespace QuanLyHoSo.Infrastructure.Network
{
    public sealed class LanServerUnavailableException : Exception
    {
        public LanServerUnavailableException(string adminServerUrl, Exception innerException)
            : base(
                $"Không kết nối được máy server/admin tại {adminServerUrl}. Vui lòng kiểm tra máy server đã bật app QuanLyHoSo, cùng mạng LAN và firewall cho phép cổng 5055.",
                innerException)
        {
            AdminServerUrl = adminServerUrl;
        }

        public string AdminServerUrl { get; }
    }
}
