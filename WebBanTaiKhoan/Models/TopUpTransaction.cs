using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Thêm thư viện này

namespace WebBanTaiKhoan.Models
{
    public class TopUpTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Chỉ định rõ kiểu dữ liệu tiền tệ để tránh lỗi SQL
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Dùng [NotMapped] để Entity Framework không cố gắng tạo thêm cột 'Date' trong Database
        // Vì 'Date' và 'CreatedAt' lúc này là một
        [NotMapped]
        public DateTime Date
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        public string Method { get; set; } = "Chuyển khoản Ngân hàng";

        public string Status { get; set; } = "Pending";

        public string? TransactionCode { get; set; }
    }
}