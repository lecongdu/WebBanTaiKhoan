using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // ID của người mua (liên kết với bảng Users)
        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        // Lưu thông tin tóm tắt lúc mua (để dự phòng)
        public string? SoldAccountInfo { get; set; }

        // --- BỔ SUNG QUAN TRỌNG TẠI ĐÂY ---
        // Một đơn hàng có thể chứa nhiều tài khoản (nếu khách mua số lượng > 1)
        // Dòng này giúp bạn dùng lệnh .Include(o => o.AccountItems) trong Controller
        public virtual ICollection<AccountItem> AccountItems { get; set; } = new List<AccountItem>();
    }
}