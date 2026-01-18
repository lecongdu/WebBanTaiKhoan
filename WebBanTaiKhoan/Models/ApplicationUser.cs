using Microsoft.AspNetCore.Identity;

namespace WebBanTaiKhoan.Models
{
    // ❌ SAI: public class ApplicationUser : ApplicationUser
    // ✅ ĐÚNG: Phải kế thừa từ IdentityUser gốc của hệ thống
    public class ApplicationUser : IdentityUser
    {
        // Thêm túi tiền (Số dư)
        public decimal Balance { get; set; } = 0;

        // Thêm Tên đầy đủ (Hiển thị cho đẹp thay vì Email)
        public string? FullName { get; set; }
    }
}