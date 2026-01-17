namespace WebBanTaiKhoan.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public decimal Balance { get; set; } // Số dư hiện tại
        public decimal TotalDeposit { get; set; } // Tổng tiền đã nạp
    }
}