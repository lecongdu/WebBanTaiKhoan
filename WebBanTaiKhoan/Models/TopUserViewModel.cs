namespace WebBanTaiKhoan.Models
{
    public class TopUserViewModel
    {
        // Tên hiển thị (đã được ẩn danh như ducon***)
        public string DisplayName { get; set; } = string.Empty;

        // Số dư của người dùng
        public decimal Balance { get; set; }
    }
}