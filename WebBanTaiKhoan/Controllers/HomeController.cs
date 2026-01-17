using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Identity;

namespace WebBanTaiKhoan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. TRANG CHỦ
        // ==========================================
        public async Task<IActionResult> Index(string search, int? categoryId)
        {
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            ViewBag.Settings = settings ?? new SystemSetting { BannerUrl = "/images/default-banner.jpg" };

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

        // ==========================================
        // 2. CHI TIẾT SẢN PHẨM
        // ==========================================
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

        // ==========================================
        // 3. TRANG LIÊN HỆ (MỚI THÊM)
        // ==========================================
        public async Task<IActionResult> Contact()
        {
            // Lấy thông tin liên hệ và bản đồ từ Database để hiển thị ra View
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            return View(settings);
        }

        // ==========================================
        // 4. TRANG NẠP TIỀN
        // ==========================================
        public async Task<IActionResult> Deposit()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/Identity/Account/Login");
            }

            var banks = await _context.BankAccounts.AsNoTracking().ToListAsync();
            return View(banks ?? new List<BankAccount>());
        }

        // ==========================================
        // 5. XỬ LÝ NẠP TIỀN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBanking(decimal amount)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (amount < 10000)
            {
                TempData["Error"] = "Số tiền nạp tối thiểu là 10.000 VNĐ.";
                return RedirectToAction(nameof(Deposit));
            }

            try
            {
                var transaction = new TopUpTransaction
                {
                    UserId = userId,
                    Amount = amount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    TransactionCode = "NAP" + DateTime.Now.ToString("ddMMHHmm") + userId.Substring(Math.Max(0, userId.Length - 4)).ToUpper(),
                    Method = "Chuyển khoản VietQR"
                };

                _context.TopUpTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Gửi yêu cầu thành công! Admin sẽ duyệt tiền cho bạn sớm nhất.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction(nameof(Deposit));
        }
    }
}