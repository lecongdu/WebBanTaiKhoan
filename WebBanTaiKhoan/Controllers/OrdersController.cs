using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==================================================
        // 🔥 1. CHỨC NĂNG MUA NGAY (ĐÃ FIX LỖI ÂM TIỀN) 🔥
        // ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            // 1. Lấy User và Sản phẩm
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            var product = await _context.Products
                .Include(p => p.AccountItems) // Load kho
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // 2. Tính toán tiền nong
            decimal totalAmount = product.Price * quantity;

            // 3. 🔥 KIỂM TRA SỐ DƯ (QUAN TRỌNG NHẤT) 🔥
            if (user.Balance < totalAmount)
            {
                TempData["Error"] = $"Số dư không đủ! Cần {totalAmount:N0}đ nhưng bạn chỉ có {user.Balance:N0}đ.";
                return RedirectToAction("Deposit", "Home"); // Chuyển ngay sang trang nạp tiền
            }

            // 4. Kiểm tra tồn kho (Lấy n account chưa bán)
            var itemsToSell = product.AccountItems
                                     .Where(x => !x.IsSold)
                                     .Take(quantity)
                                     .ToList();

            if (itemsToSell.Count < quantity)
            {
                TempData["Error"] = $"Kho chỉ còn {itemsToSell.Count} tài khoản, không đủ số lượng bạn mua.";
                return RedirectToAction("ProductDetail", "Home", new { id = productId });
            }

            // 5. Bắt đầu giao dịch (Khi đã đủ tiền và đủ hàng)
            try
            {
                // Trừ tiền
                user.Balance -= totalAmount;

                // Tạo đơn hàng
                var order = new Order
                {
                    UserId = user.Id,
                    ProductId = product.Id,
                    Price = product.Price, // Giá đơn vị
                    TotalAmount = totalAmount, // Tổng tiền
                    Status = "Completed",
                    CreatedAt = DateTime.Now,
                    OrderCode = "DH" + DateTime.Now.Ticks.ToString()[^6..],
                    AccountItems = itemsToSell // Gán danh sách tài khoản cho đơn hàng
                };

                // Đánh dấu các tài khoản là đã bán
                foreach (var item in itemsToSell)
                {
                    item.IsSold = true;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Mua thành công {quantity} tài khoản!";
                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi xử lý: " + ex.Message;
                return RedirectToAction("ProductDetail", "Home", new { id = productId });
            }
        }

        // ==================================================
        // 2. LỊCH SỬ MUA HÀNG
        // ==================================================
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.AccountItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ==================================================
        // 3. LỊCH SỬ NẠP TIỀN
        // ==================================================
        public async Task<IActionResult> DepositHistory()
        {
            var userId = _userManager.GetUserId(User);
            var topups = await _context.TopUpTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(topups);
        }

        // ==================================================
        // 4. ADMIN: QUẢN LÝ ĐƠN HÀNG
        // ==================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var allOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.AccountItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(allOrders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.AccountItems)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn hàng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================================================
        // 5. XUẤT EXCEL
        // ==================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Product)
                .Include(o => o.AccountItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("BaoCaoDonHang");

                // Header
                ws.Cells[1, 1].Value = "Mã Đơn";
                ws.Cells[1, 2].Value = "Khách Hàng";
                ws.Cells[1, 3].Value = "Ngày Mua";
                ws.Cells[1, 4].Value = "Sản Phẩm";
                ws.Cells[1, 5].Value = "Tổng Tiền";
                ws.Cells[1, 6].Value = "Chi tiết Account (Data)";

                using (var range = ws.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var o in orders)
                {
                    ws.Cells[row, 1].Value = o.OrderCode;
                    ws.Cells[row, 2].Value = o.User?.UserName ?? "Ẩn danh";
                    ws.Cells[row, 3].Value = o.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                    ws.Cells[row, 4].Value = o.Product?.Name;
                    ws.Cells[row, 5].Value = o.TotalAmount; // Dùng TotalAmount thay vì Price
                    ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";

                    // Nối chuỗi tài khoản nếu mua nhiều
                    var accData = o.AccountItems != null
                        ? string.Join(" | ", o.AccountItems.Select(a => a.Data))
                        : "Không có dữ liệu";

                    ws.Cells[row, 6].Value = accData;
                    row++;
                }

                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
}