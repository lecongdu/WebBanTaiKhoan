namespace WebBanTaiKhoan.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        // --- THÊM 2 DÒNG NÀY ĐỂ HẾT LỖI CS0117 ---
        public decimal TotalDeposit { get; set; } // Tổng tiền nạp
        public decimal Balance { get; set; }      // Số dư hiện tại
    }
}