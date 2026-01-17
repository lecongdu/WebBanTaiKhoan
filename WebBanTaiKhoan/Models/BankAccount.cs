using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class BankAccount
    {
        [Key]
        public int Id { get; set; }
        public string BankName { get; set; } = string.Empty; // Tên ngân hàng (MB, VCB...)
        public string AccountNumber { get; set; } = string.Empty; // Số tài khoản
        public string AccountName { get; set; } = string.Empty; // Tên chủ thẻ
        public string LogoUrl { get; set; } = string.Empty; // Link logo ngân hàng
        public string QrUrl { get; set; } = string.Empty; // Link ảnh mã QR
    }
}