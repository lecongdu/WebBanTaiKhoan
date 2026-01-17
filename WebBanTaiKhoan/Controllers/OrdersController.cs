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
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==================================================
        // 1. DÀNH CHO NGƯỜI DÙNG: LỊCH SỬ MUA TÀI KHOẢN
        // ==================================================
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Chỉ lấy đơn hàng (Lịch sử mua)
            var orders = await _context.Orders
                .Include(o => o.AccountItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ==================================================
        // 2. DÀNH CHO NGƯỜI DÙNG: LỊCH SỬ NẠP TIỀN (MỚI THÊM)
        // ==================================================
        public async Task<IActionResult> DepositHistory()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Chỉ lấy lịch sử nạp tiền
            var topups = await _context.TopUpTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .ToListAsync();

            return View(topups);
        }

        // ==================================================
        // 3. DÀNH CHO ADMIN: QUẢN LÝ ĐƠN HÀNG
        // ==================================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var allOrders = await _context.Orders
                .Include(o => o.AccountItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(allOrders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
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
        // 4. XUẤT EXCEL BÁO CÁO
        // ==================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var orders = await _context.Orders
                .Include(o => o.AccountItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("BaoCaoDonHang");

                using (var range = ws.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws.Cells[1, 1].Value = "Mã Đơn";
                ws.Cells[1, 2].Value = "Ngày Mua";
                ws.Cells[1, 3].Value = "Số Tiền";
                ws.Cells[1, 4].Value = "Tài Khoản | Mật Khẩu";
                ws.Cells[1, 5].Value = "Ghi chú nhanh";

                int row = 2;
                foreach (var o in orders)
                {
                    ws.Cells[row, 1].Value = "#" + o.Id;
                    ws.Cells[row, 2].Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    ws.Cells[row, 3].Value = o.TotalAmount;

                    var accDetails = o.AccountItems != null && o.AccountItems.Any()
                                     ? string.Join(" , ", o.AccountItems.Select(a => a.Data))
                                     : o.SoldAccountInfo;

                    ws.Cells[row, 4].Value = accDetails;
                    row++;
                }

                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_DonHang_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
}