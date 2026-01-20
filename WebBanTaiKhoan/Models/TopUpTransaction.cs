using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanTaiKhoan.Models
{
    public class TopUpTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Thiết lập mối quan hệ với bảng User để lấy tên khách hàng trong Admin
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public DateTime Date
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        public string Method { get; set; } = "Chuyển khoản VietQR";

        /// <summary>
        /// TRẠNG THÁI GIAO DỊCH:
        /// Pending: Hệ thống đã tạo QR (Khách chưa ấn xác nhận) hoặc Khách vừa gửi thẻ cào
        /// Processing: Khách đã bấm nút "Xác nhận đã chuyển tiền" (dành cho Bank)
        /// Success: Admin đã duyệt và cộng tiền thành công
        /// Cancelled: Giao dịch bị hủy hoặc quá thời gian
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? TransactionCode { get; set; }

        // Ghi chú của Admin khi duyệt (Ví dụ: "Đã khớp tiền Techcombank")
        public string? AdminNote { get; set; }

        // ==========================================
        // 🟢 MỚI THÊM: PHỤC VỤ NẠP THẺ CÀO
        // ==========================================

        [Display(Name = "Số Seri")]
        public string? Serial { get; set; }

        [Display(Name = "Mã thẻ (Pin)")]
        public string? Pin { get; set; }
    }
}