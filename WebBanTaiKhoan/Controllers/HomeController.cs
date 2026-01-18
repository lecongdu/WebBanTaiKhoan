using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace WebBanTaiKhoan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. TRANG CHỦ
        public async Task<IActionResult> Index(string search, int? categoryId)
        {
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Settings = settings ?? new SystemSetting
            {
                BannerUrl = "/images/banners/default.jpg",
                MarqueeText = "Chào mừng bạn đến với Shop Account uy tín!"
            };

            ViewBag.Categories = await _context.Category.AsNoTracking().ToListAsync();

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.AccountItems)
                .AsNoTracking()
                .AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId);
                ViewBag.CurrentCategoryId = categoryId;
            }

            if (!string.IsNullOrEmpty(search))
            {
                string lowerSearch = search.ToLower();
                productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(lowerSearch));
                ViewBag.CurrentSearch = search;
            }

            var results = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();
            return View(results);
        }

        // 2. CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.AccountItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            ViewBag.StockCount = product.AccountItems.Count(a => !a.IsSold);
            return View(product);
        }

        // 3. TRANG LIÊN HỆ
        public async Task<IActionResult> Contact()
        {
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            return View(settings);
        }

        // 4. TRANG NẠP TIỀN
        [Authorize]
        public async Task<IActionResult> Deposit()
        {
            var userId = _userManager.GetUserId(User);

            var pendingTrans = await _context.TopUpTransactions
                .Where(t => t.UserId == userId && t.Status == "Pending" && t.CreatedAt > DateTime.Now.AddMinutes(-30))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            ViewBag.PendingTrans = pendingTrans;

            var banks = await _context.BankAccounts.AsNoTracking().ToListAsync();
            return View(banks ?? new List<BankAccount>());
        }

        // 5. TẠO LỆNH NẠP CHỜ
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePendingDeposit(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập" });
            if (amount < 10000) return Json(new { success = false, message = "Tối thiểu 10.000đ" });

            try
            {
                var existing = await _context.TopUpTransactions
                    .AnyAsync(t => t.UserId == user.Id && t.Amount == amount && t.Status == "Pending" && t.CreatedAt > DateTime.Now.AddMinutes(-10));

                if (!existing)
                {
                    string username = user.UserName ?? "KHACH";
                    string codeNap = System.Text.RegularExpressions.Regex.Replace(username.Split('@')[0], "[^a-zA-Z0-9]", "").ToUpper();
                    string transCode = $"NAP {codeNap}";

                    var transaction = new TopUpTransaction
                    {
                        UserId = user.Id,
                        Amount = amount,
                        Status = "Pending",
                        TransactionCode = transCode,
                        Method = "Chuyển khoản VietQR",
                        CreatedAt = DateTime.Now
                    };

                    _context.TopUpTransactions.Add(transaction);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 5.2 XỬ LÝ XÁC NHẬN ĐÃ CHUYỂN
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBanking(decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var trans = await _context.TopUpTransactions
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Status == "Pending");

            if (trans != null)
            {
                trans.Status = "Processing";
                await _context.SaveChangesAsync();

                TempData["DepositSuccess"] = true;
                TempData["DepositAmount"] = amount.ToString("#,##0");
            }
            else
            {
                string codeNap = System.Text.RegularExpressions.Regex.Replace(user.UserName.Split('@')[0], "[^a-zA-Z0-9]", "").ToUpper();
                _context.TopUpTransactions.Add(new TopUpTransaction
                {
                    UserId = user.Id,
                    Amount = amount,
                    Status = "Processing",
                    TransactionCode = $"NAP {codeNap}",
                    Method = "Chuyển khoản VietQR",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["DepositSuccess"] = true;
                TempData["DepositAmount"] = amount.ToString("#,##0");
            }

            return RedirectToAction(nameof(Deposit));
        }

        // ==========================================
        // 6. BẢNG XẾP HẠNG ĐẠI GIA (SỬA LẠI: THEO TỔNG NẠP)
        // ==========================================
        public async Task<IActionResult> TopDeposit()
        {
            // Lấy danh sách User kèm theo Tổng số tiền đã nạp thành công (Status = Success)
            var topUsers = await _userManager.Users
                .Select(u => new
                {
                    OriginalName = u.UserName,
                    // Sum số tiền từ bảng TopUpTransactions nơi Status là Success
                    TotalDeposited = _context.TopUpTransactions
                        .Where(t => t.UserId == u.Id && t.Status == "Success")
                        .Sum(t => (decimal?)t.Amount) ?? 0m
                })
                .Where(x => x.TotalDeposited > 0) // Chỉ hiện những người đã từng nạp
                .OrderByDescending(x => x.TotalDeposited)
                .Take(10)
                .ToListAsync();

            // Format lại tên và trả về View
            var results = topUsers.Select(u => new {
                DisplayName = u.OriginalName.Length > 3 ? u.OriginalName.Substring(0, 3) + "***" : u.OriginalName + "***",
                Balance = u.TotalDeposited // Gán TotalDeposited vào biến Balance để không phải sửa View nhiều
            }).ToList();

            return View(results);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}