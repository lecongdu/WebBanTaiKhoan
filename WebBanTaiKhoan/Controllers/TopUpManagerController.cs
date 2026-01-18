using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Identity; // Cần thêm để dùng UserManager

namespace WebBanTaiKhoan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TopUpManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Thêm UserManager

        // Cập nhật Constructor để nhận UserManager
        public TopUpManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.TopUpTransactions
                                     .OrderByDescending(t => t.CreatedAt)
                                     .ToListAsync();
            return View(list);
        }

        // 2. DUYỆT ĐƠN (Approve) - ĐÃ SỬA LỖI CỘNG TIỀN
        public async Task<IActionResult> Approve(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);

            if (transaction != null && transaction.Status == "Pending")
            {
                // Tìm người dùng bằng UserManager để đảm bảo tính đồng bộ của Identity
                var user = await _userManager.FindByIdAsync(transaction.UserId);

                if (user != null)
                {
                    // THỰC HIỆN CỘNG TIỀN VÀO CỘT BALANCE
                    user.Balance += transaction.Amount;

                    // Cập nhật trạng thái giao dịch
                    transaction.Status = "Success";

                    // Lưu thay đổi của User và Giao dịch
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        _context.Update(transaction);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = $"Đã cộng {transaction.Amount:N0}đ vào ví của {user.Email}.";
                    }
                    else
                    {
                        TempData["Error"] = "Lỗi khi cập nhật số dư người dùng.";
                    }
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy người dùng này.";
                }
            }
            return RedirectToAction("Index");
        }

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