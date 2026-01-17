using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize]
    public class DepositController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepositController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang chủ nạp tiền
        public async Task<IActionResult> Index()
        {
            var bankAccounts = await _context.BankAccounts.ToListAsync();
            return View(bankAccounts);
        }

        // 2. Nạp thẻ cào (Test: 1111 -> Thành công luôn)
        [HttpPost]
        public async Task<IActionResult> ProcessCard(string cardCode, int amount)
        {
            if (cardCode == "1111")
            {
                // Thẻ cào test thì cho Success luôn để bạn test
                await AddMoneyToUser(amount, "Thẻ cào (Test)", "Success");
                TempData["Success"] = $"Nạp thẻ thành công {amount:N0}đ!";
            }
            else
            {
                TempData["Error"] = "Mã thẻ sai hoặc đã sử dụng!";
            }
            return RedirectToAction("Index");
        }

        // 3. Chuyển khoản (SỬA ĐỔI: Chuyển thành Pending - Chờ duyệt)
        [HttpPost]
        public async Task<IActionResult> ProcessBanking(int amount)
        {
            // Lưu trạng thái là Pending
            await AddMoneyToUser(amount, "Chuyển khoản Banking", "Pending");

            // Thông báo khách đợi
            TempData["Success"] = $"Đã gửi yêu cầu nạp {amount:N0}đ. Vui lòng chờ Admin duyệt (1-5 phút)!";
            return RedirectToAction("Index");
        }

        // Hàm chung: Lưu giao dịch vào Database
        private async Task AddMoneyToUser(decimal amount, string method, string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transaction = new TopUpTransaction
            {
                UserId = userId,
                Amount = amount,
                Date = DateTime.Now,
                Method = method,
                Status = status // Lưu trạng thái (Pending/Success)
            };
            _context.TopUpTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}