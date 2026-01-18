using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace WebBanTaiKhoan.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ======================================================
        // XỬ LÝ MUA NGAY (1 SẢN PHẨM)
        // ======================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int id)
        {
            // 1. Lấy User từ DB để đảm bảo số dư mới nhất
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            // 2. Tìm tài khoản chưa bán
            var account = await _context.AccountItems
                .Include(a => a.Product)
                .Where(a => a.ProductId == id && a.IsSold == false)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                TempData["Error"] = "Sản phẩm này tạm thời hết hàng!";
                return RedirectToAction("ProductDetail", "Home", new { id = id });
            }

            decimal price = account.Product.Price;

            // 3. Kiểm tra số dư thực tế
            if (user.Balance < price)
            {
                TempData["Error"] = $"Số dư không đủ! Bạn có {user.Balance:N0}đ, cần {price:N0}đ.";
                return RedirectToAction("Deposit", "Home");
            }

            // ======================================================
            // SỬ DỤNG TRANSACTION ĐỂ ĐẢM BẢO AN TOÀN DỮ LIỆU
            // ======================================================
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Trừ tiền và CẬP NHẬT DATABASE (Quan trọng)
                user.Balance -= price;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    throw new Exception("Không thể cập nhật số dư thành viên.");
                }

                // 5. Đánh dấu tài khoản đã bán
                account.IsSold = true;

                // 6. Tạo đơn hàng
                var order = new Order
                {
                    UserId = user.Id,
                    ProductId = account.ProductId,
                    Price = price,
                    TotalAmount = price,
                    OrderCode = "DH" + Guid.NewGuid().ToString()[..6].ToUpper(),
                    Status = "Completed",
                    CreatedAt = DateTime.Now,
                    AccountItems = new List<AccountItem> { account }
                };

                _context.Orders.Add(order);

                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // Xác nhận hoàn tất giao dịch
                await dbTransaction.CommitAsync();

                TempData["Success"] = "Mua hàng thành công!";
                return RedirectToAction("Success", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                // Nếu có bất kỳ lỗi nào, hủy bỏ mọi thay đổi (Rollback)
                await dbTransaction.RollbackAsync();

                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("ProductDetail", "Home", new { id = id });
            }
        }

        [Authorize]
        public async Task<IActionResult> Success(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var order = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.AccountItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}