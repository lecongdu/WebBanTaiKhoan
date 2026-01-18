using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanTaiKhoan.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string OrderCode { get; set; } // Mã đơn hàng (VD: DH6383...)

        // --- LIÊN KẾT NGƯỜI DÙNG ---
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // --- LIÊN KẾT SẢN PHẨM (Đây là phần bạn đang thiếu gây lỗi) ---
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // --- THÔNG TIN GIÁ & TRẠNG THÁI (Đang thiếu) ---
        public decimal Price { get; set; }       // Giá gốc 1 sản phẩm
        public decimal TotalAmount { get; set; } // Tổng tiền (Giá x Số lượng)

        public string Status { get; set; } = "Completed"; // Trạng thái đơn

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- QUAN HỆ 1-NHIỀU (1 Đơn hàng chứa nhiều Acc) ---
        public virtual ICollection<AccountItem> AccountItems { get; set; } = new List<AccountItem>();
    }
}