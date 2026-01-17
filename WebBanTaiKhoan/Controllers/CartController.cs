using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebBanTaiKhoan.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string CART_KEY = "ShopCart";

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CART_KEY);
            return string.IsNullOrEmpty(sessionData)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(sessionData);
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_KEY, JsonConvert.SerializeObject(cart));
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            var stockCount = await _context.AccountItems
                .CountAsync(a => a.ProductId == productId && !a.IsSold);

            if (item != null)
            {
                if (item.Quantity + quantity > stockCount)
                {
                    TempData["Error"] = "Số lượng trong giỏ hàng đã đạt tối đa kho!";
                    item.Quantity = stockCount;
                }
                else
                {
                    item.Quantity += quantity;
                }
            }
            else
            {
                if (quantity > stockCount) quantity = stockCount;
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Index() => View(GetCartItems());

        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    var stockCount = await _context.AccountItems
                        .CountAsync(a => a.ProductId == id && !a.IsSold);

                    if (quantity > stockCount)
                    {
                        TempData["Error"] = $"Chỉ còn {stockCount} tài khoản!";
                        item.Quantity = stockCount;
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCartItems();
            cart.RemoveAll(p => p.ProductId == id);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // ==========================================
        // HÀM THANH TOÁN: ĐÃ SỬA ĐỂ HIỆN TÀI KHOẢN NHẬP SỈ
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            var userId = _userManager.GetUserId(User);

            // Duyệt qua từng loại sản phẩm trong giỏ
            foreach (var item in cart)
            {
                // Lấy ra danh sách tài khoản chưa bán
                var accounts = await _context.AccountItems
                    .Where(a => a.ProductId == item.ProductId && a.IsSold == false)
                    .Take(item.Quantity).ToListAsync();

                if (accounts.Count < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.ProductName} vừa hết hàng!";
                    return RedirectToAction("Index");
                }

                foreach (var acc in accounts)
                {
                    // Đánh dấu đã bán
                    acc.IsSold = true;

                    // LOGIC THÔNG MINH: 
                    // Nếu trường Data có dữ liệu (nhập sỉ) thì lấy Data.
                    // Nếu không thì lấy Username | Password (nhập lẻ).
                    string info = !string.IsNullOrEmpty(acc.Data)
                                  ? acc.Data
                                  : $"{acc.Username} | {acc.Password}";

                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        TotalAmount = item.Price,
                        SoldAccountInfo = info // Lưu thông tin cuối cùng vào đơn hàng
                    };

                    _context.Orders.Add(order);

                    // Gán OrderId ngược lại cho AccountItem để truy xuất lịch sử chính xác
                    // (Lệnh SaveChanges bên dưới sẽ giúp lấy ID tự tăng của Order)
                    acc.Order = order;
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(CART_KEY);
            TempData["Success"] = "Thanh toán thành công! Vui lòng kiểm tra tài khoản đã mua.";

            return RedirectToAction("MyOrders", "Orders");
        }
    }
}