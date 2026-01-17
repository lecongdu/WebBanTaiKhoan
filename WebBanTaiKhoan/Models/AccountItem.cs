using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanTaiKhoan.Models
{
    public class AccountItem
    {
        [Key]
        public int Id { get; set; }

        // --- DÙNG CHO NHẬP LẺ TỪNG CÁI ---
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;

        // --- DÙNG CHO NHẬP KHO HÀNG LOẠT ---
        // Lưu định dạng "email|pass". Khi hiển thị chỉ cần dùng lệnh .Split('|')
        public string Data { get; set; } = string.Empty;

        // Thời gian nhập kho
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Trạng thái: False = Còn hàng, True = Đã bán
        public bool IsSold { get; set; } = false;

        // --- LIÊN KẾT VỚI SẢN PHẨM ---
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // --- BỔ SUNG QUAN TRỌNG: LIÊN KẾT VỚI ĐƠN HÀNG ---
        // Khi khách mua, hệ thống sẽ gán Id của đơn hàng vào đây để biết tài khoản này thuộc về ai
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}