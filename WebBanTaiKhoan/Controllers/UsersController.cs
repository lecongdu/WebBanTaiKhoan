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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // 1. HIỂN THỊ DANH SÁCH THÀNH VIÊN (Đã đồng bộ UserRoleViewModel)
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var userList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                // Lấy quyền của người dùng
                var roles = await _userManager.GetRolesAsync(user);

                // Tính tổng nạp thành công
                decimal totalDeposit = await _context.TopUpTransactions
                    .AsNoTracking()
                    .Where(t => t.UserId == user.Id && t.Status == "Success" && t.Amount > 0)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                userList.Add(new UserRoleViewModel
                {
                    UserId = user.Id, // Đã đổi từ Id sang UserId cho khớp Model
                    UserName = user.UserName,
                    Email = user.Email,
                    TotalDeposit = totalDeposit,
                    Balance = user.Balance,
                    Role = roles.FirstOrDefault() ?? "Khách" // Lấy quyền hiện tại
                });
            }

            return View(userList);
        }

        // 2. HÀM CỘNG/TRỪ TIỀN (Giữ nguyên Transaction an toàn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBalance(string userId, decimal amount, string type)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (amount <= 0)
            {
                TempData["Error"] = "Số tiền phải lớn hơn 0!";
                return RedirectToAction(nameof(Index));
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string actionType = type?.ToLower() ?? "plus";
                decimal oldBalance = user.Balance;

                if (actionType == "minus")
                {
                    if (user.Balance < amount)
                    {
                        TempData["Error"] = $"Số dư không đủ! (Hiện có: {user.Balance:N0}đ)";
                        return RedirectToAction(nameof(Index));
                    }
                    user.Balance -= amount;
                }
                else
                {
                    user.Balance += amount;
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) throw new Exception("Không thể cập nhật User vào DB.");

                var transaction = new TopUpTransaction
                {
                    UserId = userId,
                    Amount = (actionType == "minus") ? -amount : amount,
                    Status = "Success",
                    TransactionCode = (actionType == "minus" ? "AD_TRU_" : "AD_CONG_") +
                                     $"{oldBalance:N0}->{user.Balance:N0}_" +
                                     Guid.NewGuid().ToString()[..4].ToUpper(),
                    CreatedAt = DateTime.Now
                };

                _context.TopUpTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                TempData["Success"] = $"Đã {(actionType == "minus" ? "TRỪ" : "CỘNG")} {amount:N0}đ cho {user.UserName}. Số dư mới: {user.Balance:N0}đ";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. XÓA THÀNH VIÊN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.UserName == User.Identity.Name)
            {
                TempData["Error"] = "Bạn không thể tự xóa chính mình!";
                return RedirectToAction(nameof(Index));
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transactions = _context.TopUpTransactions.Where(t => t.UserId == id);
                _context.TopUpTransactions.RemoveRange(transactions);

                var orders = _context.Orders.Where(o => o.UserId == id);
                _context.Orders.RemoveRange(orders);

                await _context.SaveChangesAsync();

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded) throw new Exception("Lỗi khi xóa User.");

                await dbTransaction.CommitAsync();
                TempData["Success"] = $"Đã xóa sạch dữ liệu của {user.UserName}.";
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}