using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // 1. HIỂN THỊ DANH SÁCH THÀNH VIÊN KÈM SỐ DƯ
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                // Tính tổng nạp (Trạng thái thành công)
                decimal totalDeposit = _context.TopUpTransactions
                    .Where(t => t.UserId == user.Id && t.Status == "Success")
                    .Sum(t => (decimal?)t.Amount) ?? 0;

                // Tính tổng chi (Các đơn hàng đã mua)
                decimal totalSpent = _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;

                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    TotalDeposit = totalDeposit,
                    Balance = totalDeposit - totalSpent
                });
            }

            return View(userList);
        }

        // 2. HÀM CỘNG/TRỪ TIỀN (SỬA SỐ DƯ)
        [HttpPost]
        public async Task<IActionResult> UpdateBalance(string userId, decimal amount, string type)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Đảm bảo số tiền nạp là số dương
            if (amount <= 0)
            {
                TempData["Error"] = "Số tiền phải lớn hơn 0!";
                return RedirectToAction(nameof(Index));
            }

            // Tạo một bản ghi giao dịch mới để lưu vết lịch sử
            var transaction = new TopUpTransaction
            {
                UserId = userId,
                Amount = amount,
                Status = "Success",
                // Tạo mã giao dịch để Admin dễ nhận biết
                TransactionCode = (type == "Plus" ? "ADMIN_CONG_" : "ADMIN_TRU_") + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CreatedAt = DateTime.Now
            };

            _context.TopUpTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(type == "Plus" ? "CỘNG" : "TRỪ")} {amount.ToString("#,##0")}đ cho thành viên {user.UserName}";
            return RedirectToAction(nameof(Index));
        }

        // 3. XÓA THÀNH VIÊN
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Kiểm tra nếu admin tự xóa chính mình
                if (user.UserName == User.Identity.Name)
                {
                    TempData["Error"] = "Bạn không thể tự xóa tài khoản admin của chính mình!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Đã xóa thành viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}