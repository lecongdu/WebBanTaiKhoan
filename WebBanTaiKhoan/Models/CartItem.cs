using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class CartItem
    {
        [Key] // Khóa chính để lưu vào DB
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // ID của người dùng (liên kết với AspNetUsers)

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        // Trường này không lưu xuống DB mà chỉ dùng để tính toán hiển thị
        public decimal Total => Price * Quantity;
    }
}