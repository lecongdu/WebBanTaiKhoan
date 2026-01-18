using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string WEBHOOK_SECRET = "Mã_bí_mật_của_bạn_ở_đây"; // Dùng để xác thực từ bên nạp tiền

        public PaymentWebhookController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("receive")]
        public async Task<IActionResult> HandlePayment([FromBody] PaymentRequest request)
        {
            // 1. KIỂM TRA BẢO MẬT (Tránh hacker gửi request ảo)
            // if (request.SecretKey != WEBHOOK_SECRET) return Unauthorized();

            if (request == null || string.IsNullOrEmpty(request.Content))
                return BadRequest("Dữ liệu không hợp lệ");

            // 2. PHÂN TÍCH NỘI DUNG CHUYỂN KHOẢN (Ví dụ: "NAP 12345")
            // Giả sử nội dung nạp là: NAP [USER_ID]
            string content = request.Content.ToUpper();
            string userId = ExtractUserIdFromContent(content); // Hàm tự viết để tách ID từ nội dung

            if (string.IsNullOrEmpty(userId)) return BadRequest("Nội dung không chứa ID người dùng");

            // 3. KIỂM TRA GIAO DỊCH ĐÃ TỒN TẠI CHƯA (Tránh nạp trùng 2 lần)
            var existingTran = await _context.TopUpTransactions
                .AnyAsync(t => t.TransactionCode == request.TranId);

            if (existingTran) return Ok("Giao dịch đã được xử lý trước đó");

            // 4. THỰC HIỆN CỘNG TIỀN
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Bắt đầu Transaction để đảm bảo an toàn dữ liệu
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Cập nhật số dư trực tiếp trong bảng Users
                    user.Balance += request.Amount;
                    var updateResult = await _userManager.UpdateAsync(user);

                    if (updateResult.Succeeded)
                    {
                        // B. Ghi lịch sử giao dịch vào bảng TopUpTransactions
                        var transaction = new TopUpTransaction
                        {
                            UserId = userId,
                            Amount = request.Amount,
                            Status = "Success",
                            TransactionCode = request.TranId, // Mã giao dịch từ Ngân hàng
                            CreatedAt = DateTime.Now
                        };
                        _context.TopUpTransactions.Add(transaction);
                        await _context.SaveChangesAsync();

                        await dbTransaction.CommitAsync();
                        return Ok(new { success = true, message = "Cộng tiền thành công" });
                    }
                }
                catch (Exception)
                {
                    await dbTransaction.RollbackAsync();
                    return StatusCode(500, "Lỗi khi xử lý Database");
                }
            }

            return NotFound("Không tìm thấy người dùng");
        }

        private string ExtractUserIdFromContent(string content)
        {
            // Logic tách ID: Ví dụ nội dung là "NAP 101", ta lấy số 101
            // Tùy theo cách bạn yêu cầu khách ghi nội dung mà viết hàm này
            try
            {
                return content.Replace("NAP", "").Trim();
            }
            catch { return ""; }
        }
    }

    // Lớp nhận dữ liệu từ các bên QR (Tùy bên bạn dùng mà sửa tên thuộc tính cho khớp)
    public class PaymentRequest
    {
        public string TranId { get; set; } // Mã giao dịch ngân hàng
        public decimal Amount { get; set; } // Số tiền nạp
        public string Content { get; set; } // Nội dung chuyển khoản
        public string SecretKey { get; set; }
    }
}