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

        // =========================================================
        // 0. 🟢 TRANG CHÀO (LẤY DỮ LIỆU TỪ DATABASE)
        // =========================================================
        public async Task<IActionResult> Welcome()
        {
            // Lấy bản ghi cài đặt từ Database
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();

            // Đưa vào ViewBag để trang Welcome.cshtml sử dụng
            ViewBag.WelcomeBadge = settings?.WelcomeBadge ?? "Nick3s - Uy Tín Tạo Thương Hiệu";
            ViewBag.WelcomeTitle = settings?.WelcomeTitle ?? "KHO TÀI KHOẢN SỐ<br>LỚN NHẤT VIỆT NAM";
            ViewBag.WelcomeSubTitle = settings?.WelcomeSubTitle ?? "Cung cấp tài khoản Game, Netflix, Youtube Premium, ChatGPT và các dịch vụ số bản quyền với giá rẻ nhất thị trường. Giao dịch tự động 24/7!";
            ViewBag.WelcomeButtonText = settings?.WelcomeButtonText ?? "VÀO CỬA HÀNG NGAY";

            return View();
        }

        // 1. TRANG CHỦ (SHOP)
        public async Task<IActionResult> Index(string search, int? categoryId, string welcome)
        {
            // Logic đá sang trang chào nếu chưa đăng nhập và chưa bấm nút vào shop
            if (!User.Identity.IsAuthenticated && string.IsNullOrEmpty(welcome))
            {
                return RedirectToAction("Welcome");
            }

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

            foreach (var item in results)
            {
                item.StockQuantity = item.AccountItems.Count(a => !a.IsSold);
                item.SoldQuantity = item.AccountItems.Count(a => a.IsSold);
            }

            return View(results);
        }

        // --- MỚI: TRANG SẢN PHẨM BÁN CHẠY RIÊNG BIỆT ---
        public async Task<IActionResult> BestSellers()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.AccountItems)
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in products)
            {
                item.StockQuantity = item.AccountItems.Count(a => !a.IsSold);
                item.SoldQuantity = item.AccountItems.Count(a => a.IsSold);
            }

            var results = products
                .Where(p => p.SoldQuantity > 0)
                .OrderByDescending(p => p.SoldQuantity)
                .Take(20)
                .ToList();

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

            product.StockQuantity = product.AccountItems.Count(a => !a.IsSold);
            product.SoldQuantity = product.AccountItems.Count(a => a.IsSold);

            ViewBag.StockCount = product.StockQuantity;
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

        // --- 🔴 XỬ LÝ GỬI THẺ CÀO ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostCard(string cardType, string serial, string pin, decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            if (string.IsNullOrEmpty(serial) || string.IsNullOrEmpty(pin) || amount <= 0)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ Seri, Mã thẻ và Mệnh giá!";
                return RedirectToAction(nameof(Deposit));
            }

            var transaction = new TopUpTransaction
            {
                UserId = user.Id,
                Amount = amount,
                Status = "Pending",
                Serial = serial.Trim(),
                Pin = pin.Trim(),
                TransactionCode = $"CARD_{cardType.ToUpper()}_{DateTime.Now.ToString("ssmmHH")}",
                Method = $"Thẻ cào {cardType}",
                CreatedAt = DateTime.Now
            };

            _context.TopUpTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["DepositSuccess"] = true;
            TempData["DepositAmount"] = amount.ToString("#,##0");

            return RedirectToAction(nameof(Deposit));
        }

        // 5. TẠO LỆNH NẠP CHỜ (VIETQR)
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

        // 6. BẢNG XẾP HẠNG ĐẠI GIA
        public async Task<IActionResult> TopDeposit()
        {
            var topUsers = await _userManager.Users
                .Select(u => new
                {
                    OriginalName = u.UserName,
                    TotalDeposited = _context.TopUpTransactions
                        .Where(t => t.UserId == u.Id && t.Status == "Success")
                        .Sum(t => (decimal?)t.Amount) ?? 0m
                })
                .Where(x => x.TotalDeposited > 0)
                .OrderByDescending(x => x.TotalDeposited)
                .Take(10)
                .ToListAsync();

            var results = topUsers.Select(u => new {
                DisplayName = u.OriginalName.Length > 3 ? u.OriginalName.Substring(0, 3) + "***" : u.OriginalName + "***",
                Balance = u.TotalDeposited
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