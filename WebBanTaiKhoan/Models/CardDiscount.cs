namespace WebBanTaiKhoan.Models
{
    public class CardDiscount
    {
        public int Id { get; set; }
        public int Amount { get; set; }      // Mệnh giá (Vd: 100000)
        public int ReceiveAmount { get; set; } // Thực nhận (Vd: 82000)
    }
}