using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize] // Bắt buộc đăng nhập để dùng giỏ hàng Database
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. HIỂN THỊ GIỎ HÀNG (Lấy từ Database)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return View(cartItems);
        }

        // ==========================================
        // 2. THÊM VÀO GIỎ HÀNG (Lưu trực tiếp vào Database)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var stockCount = await _context.AccountItems.CountAsync(a => a.ProductId == productId && !a.IsSold);

            if (stockCount <= 0)
            {
                TempData["Error"] = "Sản phẩm này hiện đang hết hàng!";
                return RedirectToAction("Index");
            }

            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);

            if (item != null)
            {
                if (item.Quantity + quantity > stockCount)
                {
                    TempData["Error"] = $"Kho chỉ còn {stockCount} sản phẩm!";
                    item.Quantity = stockCount;
                }
                else
                {
                    item.Quantity += quantity;
                }
                _context.CartItems.Update(item);
            }
            else
            {
                if (quantity > stockCount) quantity = stockCount;

                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl ?? ""
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. CẬP NHẬT SỐ LƯỢNG
        // ==========================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == userId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    var stockCount = await _context.AccountItems.CountAsync(a => a.ProductId == id && !a.IsSold);
                    if (quantity > stockCount)
                    {
                        TempData["Error"] = $"Kho chỉ còn {stockCount} sản phẩm!";
                        item.Quantity = stockCount;
                    }
                    else { item.Quantity = quantity; }
                    _context.CartItems.Update(item);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. XÓA KHỎI GIỎ HÀNG
        // ==========================================
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. 🔥 MUA NGAY (QUICK BUY) - MỚI THÊM 🔥
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> QuickBuy(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null || quantity <= 0) return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

            decimal totalAmount = product.Price * quantity;

            // 🔴 KIỂM TRA SỐ DƯ
            if (user.Balance < totalAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số dư không đủ! Bạn cần nạp thêm {(totalAmount - user.Balance):#,##0}đ để chốt đơn này."
                });
            }

            // 🟡 KIỂM TRA KHO
            var accountsToSell = await _context.AccountItems
                .Where(a => a.ProductId == productId && !a.IsSold)
                .Take(quantity)
                .ToListAsync();

            if (accountsToSell.Count < quantity)
            {
                return Json(new { success = false, message = "Số lượng trong kho không đủ!" });
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Trừ tiền
                user.Balance -= totalAmount;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded) throw new Exception("Lỗi cập nhật ví.");

                // 2. Tạo đơn hàng
                var order = new Order
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Price = product.Price,
                    TotalAmount = totalAmount,
                    Status = "Completed",
                    CreatedAt = DateTime.Now,
                    OrderCode = "DHQ" + Guid.NewGuid().ToString()[..6].ToUpper(),
                    AccountItems = accountsToSell
                };

                foreach (var acc in accountsToSell) { acc.IsSold = true; }
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Json(new { success = true, message = "Mua hàng thành công!" });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi xử lý: " + ex.Message });
            }
        }

        // ==========================================
        // 6. 🔥 THANH TOÁN GIỎ HÀNG (CHECKOUT) 🔥
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var cart = await _context.CartItems.Where(c => c.UserId == user.Id).ToListAsync();
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            decimal totalCartAmount = cart.Sum(i => i.Price * i.Quantity);

            if (user.Balance < totalCartAmount)
            {
                TempData["Error"] = $"Số dư không đủ! Cần {totalCartAmount:N0}đ, hiện có {user.Balance:N0}đ.";
                return RedirectToAction("Index");
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                user.Balance -= totalCartAmount;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded) throw new Exception("Không thể cập nhật số dư thành viên.");

                foreach (var item in cart)
                {
                    var accountsToSell = await _context.AccountItems
                        .Where(a => a.ProductId == item.ProductId && !a.IsSold)
                        .Take(item.Quantity)
                        .ToListAsync();

                    if (accountsToSell.Count < item.Quantity)
                    {
                        throw new Exception($"Sản phẩm {item.ProductName} vừa hết hàng hoặc không đủ số lượng!");
                    }

                    var order = new Order
                    {
                        UserId = user.Id,
                        ProductId = item.ProductId,
                        Price = item.Price,
                        TotalAmount = item.Price * item.Quantity,
                        Status = "Completed",
                        CreatedAt = DateTime.Now,
                        OrderCode = "DH" + Guid.NewGuid().ToString()[..6].ToUpper(),
                        AccountItems = accountsToSell
                    };

                    foreach (var acc in accountsToSell) { acc.IsSold = true; }
                    _context.Orders.Add(order);
                }

                _context.CartItems.RemoveRange(cart);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["Success"] = "Thanh toán thành công!";
                return RedirectToAction("MyOrders", "Orders");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "Lỗi thanh toán: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}