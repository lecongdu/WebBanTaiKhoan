using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
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
            ViewBag.Revenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            ViewBag.Orders = await _context.Orders.CountAsync();
            ViewBag.Products = await _context.Products.CountAsync();
            ViewBag.Accounts = await _context.AccountItems.CountAsync(a => !a.IsSold);

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
        // 2. QUẢN LÝ NHÂN VIÊN
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole)) return RedirectToAction(nameof(ManageUsers));
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (newRole != "Member")
            {
                if (!await _roleManager.RoleExistsAsync(newRole)) await _roleManager.CreateAsync(new IdentityRole(newRole));
                await _userManager.AddToRoleAsync(user, newRole);
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // ==========================================
        // 3. QUẢN LÝ NẠP TIỀN
        // ==========================================
        [Authorize(Roles = "Admin")]
        [Route("Admin/TopUpManager")]
        public async Task<IActionResult> TopUpManager(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.TopUpTransactions.Include(t => t.User).AsQueryable();

            if (startDate.HasValue) query = query.Where(t => t.CreatedAt >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(t => t.CreatedAt < endDate.Value.Date.AddDays(1));

            var allTransactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            ViewBag.BankTrans = allTransactions.Where(t => string.IsNullOrWhiteSpace(t.Serial)).ToList();
            ViewBag.CardTrans = allTransactions.Where(t => !string.IsNullOrWhiteSpace(t.Serial)).ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var transaction = await _context.TopUpTransactions.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null || transaction.Status == "Success")
            {
                TempData["Error"] = "Giao dịch không hợp lệ!";
                return RedirectToAction(nameof(TopUpManager));
            }

            try
            {
                if (transaction.User != null)
                {
                    decimal actualAmount = (decimal)transaction.Amount;

                    if (!string.IsNullOrWhiteSpace(transaction.Serial))
                    {
                        int amountInt = (int)transaction.Amount;
                        var config = await _context.CardDiscounts.FirstOrDefaultAsync(d => d.Amount == amountInt);

                        if (config != null)
                        {
                            actualAmount = (decimal)config.ReceiveAmount;
                        }
                        else
                        {
                            actualAmount = (decimal)transaction.Amount * 0.8m;
                        }
                    }

                    transaction.User.Balance += actualAmount;
                    transaction.Status = "Success";
                    transaction.AdminNote = $"Duyệt mệnh giá {transaction.Amount:#,##0}đ. Thực nhận: {actualAmount:#,##0}đ. Bởi {User.Identity?.Name}";

                    _context.Users.Update(transaction.User);
                    _context.TopUpTransactions.Update(transaction);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Đã cộng {actualAmount:#,##0}đ vào ví khách hàng.";
                }
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
            if (transaction != null && transaction.Status != "Success")
            {
                transaction.Status = "Cancelled";
                transaction.AdminNote = "Admin từ chối";
                _context.TopUpTransactions.Update(transaction);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn.";
            }
            return RedirectToAction(nameof(TopUpManager));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.TopUpTransactions.FindAsync(id);
            if (transaction != null)
            {
                _context.TopUpTransactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(TopUpManager));
        }

        // ==========================================
        // 4. QUẢN LÝ CHIẾT KHẤU THẺ (CÓ TỰ KHỞI TẠO)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageDiscounts()
        {
            var discounts = await _context.CardDiscounts.OrderBy(d => d.Amount).ToListAsync();

            // 🟢 NẾU BẢNG TRỐNG -> TỰ ĐỘNG THÊM MỆNH GIÁ MẪU
            if (!discounts.Any())
            {
                var defaultValues = new List<CardDiscount>
                {
                    new CardDiscount { Amount = 10000, ReceiveAmount = 8000 },
                    new CardDiscount { Amount = 20000, ReceiveAmount = 16000 },
                    new CardDiscount { Amount = 30000, ReceiveAmount = 24000 },
                    new CardDiscount { Amount = 50000, ReceiveAmount = 40000 },
                    new CardDiscount { Amount = 100000, ReceiveAmount = 82000 },
                    new CardDiscount { Amount = 200000, ReceiveAmount = 164000 },
                    new CardDiscount { Amount = 500000, ReceiveAmount = 410000 },
                    new CardDiscount { Amount = 1000000, ReceiveAmount = 820000 }
                };
                _context.CardDiscounts.AddRange(defaultValues);
                await _context.SaveChangesAsync();
                discounts = await _context.CardDiscounts.OrderBy(d => d.Amount).ToListAsync();
            }

            return View(discounts);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDiscount(int id, int receive)
        {
            var discount = await _context.CardDiscounts.FindAsync(id);
            if (discount != null)
            {
                discount.ReceiveAmount = receive;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}