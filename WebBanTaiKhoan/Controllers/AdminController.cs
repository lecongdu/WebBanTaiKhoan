using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    // Cho phép cả Admin, Collaborator và usert vào Dashboard chung
    [Authorize(Roles = "Admin,Collaborator,usert")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // 1. TRUNG TÂM ĐIỀU KHIỂN (DASHBOARD CHUNG)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Thống kê cơ bản
            ViewBag.Revenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            ViewBag.Orders = await _context.Orders.CountAsync();
            ViewBag.Products = await _context.Products.CountAsync();
            ViewBag.Accounts = await _context.AccountItems.CountAsync(a => !a.IsSold);

            // Dữ liệu biểu đồ (Chỉ cần thiết cho Admin nhưng cứ load để tránh lỗi View)
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-6);
            var dateList = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i)).ToList();

            var dailyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            ViewBag.ChartLabels = dateList.Select(d => d.ToString("dd/MM")).ToList();
            ViewBag.ChartValues = dateList.Select(d => dailyRevenue.FirstOrDefault(r => r.Date == d)?.Total ?? 0m).ToList();

            return View();
        }

        // ==========================================
        // 2. QUẢN LÝ NHÂN VIÊN (CHỈ ADMIN)
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoleList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "Khách"
                });
            }
            return View(userRoleList);
        }

        // HÀM CHỐT: Thay đổi quyền (Fix lỗi không hoạt động)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Bước 1: Xóa sạch tất cả Role cũ
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "Lỗi khi xóa quyền cũ!";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Bước 2: Kiểm tra và tạo Role mới nếu chưa có trong hệ thống
            if (newRole != "Member") // Member coi như không có Role
            {
                if (!await _roleManager.RoleExistsAsync(newRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(newRole));
                }

                // Bước 3: Gán Role mới
                var addResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!addResult.Succeeded)
                {
                    TempData["Error"] = "Lỗi khi gán quyền mới!";
                    return RedirectToAction(nameof(ManageUsers));
                }
            }

            TempData["Success"] = $"Đã cập nhật quyền cho {user.UserName} thành {newRole}";
            return RedirectToAction(nameof(ManageUsers));
        }

        // ==========================================
        // 3. QUẢN LÝ NẠP TIỀN (CHỈ ADMIN)
        // ==========================================

        [Authorize(Roles = "Admin")]
        [Route("Admin/TopUpManager")]
        public async Task<IActionResult> TopUpManager()
        {
            var transactions = await _context.TopUpTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(transactions);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var transaction = await _context.TopUpTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null || transaction.Status == "Success")
            {
                TempData["Error"] = "Giao dịch không hợp lệ.";
                return RedirectToAction(nameof(TopUpManager));
            }

            try
            {
                transaction.User.Balance += (decimal)transaction.Amount;
                transaction.Status = "Success";
                transaction.AdminNote = $"Duyệt bởi {User.Identity?.Name} lúc {DateTime.Now:HH:mm dd/MM}";

                _context.Users.Update(transaction.User);
                _context.TopUpTransactions.Update(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã nạp tiền thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(TopUpManager));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);
            if (transaction == null || transaction.Status == "Success") return RedirectToAction(nameof(TopUpManager));

            transaction.Status = "Cancelled";
            transaction.AdminNote = "Từ chối bởi Admin";
            _context.TopUpTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy yêu cầu.";
            return RedirectToAction(nameof(TopUpManager));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);
            if (transaction != null)
            {
                _context.TopUpTransactions.Remove(transaction);
                await SaveChangesAsync();
                TempData["Success"] = "Đã xóa bản ghi.";
            }
            return RedirectToAction(nameof(TopUpManager));
        }

        private async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}