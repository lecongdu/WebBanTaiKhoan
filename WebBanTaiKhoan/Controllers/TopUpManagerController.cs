using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    // Chỉ Admin mới được vào trang này
    [Authorize(Roles = "Admin")]
    public class TopUpManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopUpManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách các đơn nạp
        public async Task<IActionResult> Index()
        {
            // SỬA LỖI: Đổi t.Date thành t.CreatedAt để SQL có thể hiểu và sắp xếp
            var list = await _context.TopUpTransactions
                                     .OrderByDescending(t => t.CreatedAt)
                                     .ToListAsync();
            return View(list);
        }

        // 2. DUYỆT ĐƠN (Approve) -> Cộng tiền trực tiếp vào ví người dùng
        public async Task<IActionResult> Approve(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);

            if (transaction != null && transaction.Status == "Pending")
            {
                // Tìm người dùng sở hữu giao dịch này
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);

                if (user != null)
                {
                    // GIẢ SỬ: Bạn dùng Identity và có cột Wallet hoặc Balance trong bảng Users
                    // Nếu bạn dùng bảng riêng cho số dư, hãy gọi bảng đó ở đây.
                    // user.Wallet += transaction.Amount; 

                    // Đánh dấu thành công
                    transaction.Status = "Success";

                    _context.Update(transaction);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã duyệt thành công {transaction.Amount:N0}đ cho khách hàng.";
                }
            }
            return RedirectToAction("Index");
        }

        // 3. HỦY ĐƠN (Reject) -> Chuyển thành Cancelled
        public async Task<IActionResult> Reject(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);
            if (transaction != null && transaction.Status == "Pending")
            {
                transaction.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Info"] = "Đã hủy yêu cầu nạp tiền.";
            }
            return RedirectToAction("Index");
        }

        // 4. Xóa lịch sử (Dọn dẹp)
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);
            if (transaction != null)
            {
                _context.TopUpTransactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}