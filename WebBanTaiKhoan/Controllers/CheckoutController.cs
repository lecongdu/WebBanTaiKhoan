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
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập để hệ thống biết ai đang mua
        [ValidateAntiForgeryToken] // Bảo mật chống giả mạo yêu cầu
        public async Task<IActionResult> BuyNow(int id)
        {
            // 1. Tìm tài khoản chưa bán kèm theo thông tin sản phẩm để lấy giá
            var account = await _context.AccountItems
                .Include(a => a.Product) // Load thông tin Product để lấy Price
                .Where(a => a.ProductId == id && a.IsSold == false)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                return Content("HẾT HÀNG! Vui lòng quay lại sau.");
            }

            // 2. Lấy thông tin User hiện tại
            var userId = _userManager.GetUserId(User);

            // 3. Thực hiện chốt đơn
            account.IsSold = true;

            var order = new Order
            {
                UserId = userId, // Lưu đúng ID để hiển thị trong MyOrders
                OrderDate = DateTime.Now,
                // Lấy giá thực tế từ sản phẩm thay vì để số 0
                TotalAmount = account.Product?.Price ?? 0,
                SoldAccountInfo = $"Tài khoản: {account.Username} | Mật khẩu: {account.Password}"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Chuyển hướng sang trang thành công
            return RedirectToAction("Success", new { orderId = order.Id });
        }

        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}