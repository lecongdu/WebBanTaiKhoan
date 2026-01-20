public class TopUpCard
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string RequestId { get; set; } // Mã để check với API
    public string CardType { get; set; } // Viettel, Vinaphone...
    public string Serial { get; set; }
    public string Code { get; set; }
    public decimal Amount { get; set; } // Mệnh giá
    public string Status { get; set; } // Pending, Success, Error
    public DateTime CreatedAt { get; set; }
}