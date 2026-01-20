using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebBanTaiKhoan.Controllers
{
    // KHÓA CỬA: Chỉ Admin và những người được bạn phân quyền (CTV) mới vào được đây
    [Authorize(Roles = "Admin,Collaborator,usert")]
    public class AccountItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // DANH SÁCH TÀI KHOẢN - TÌM KIẾM ĐA NĂNG & BỘ LỌC TRẠNG THÁI
        // =======================================================
        public async Task<IActionResult> Index(string searchString, string status)
        {
            // 1. Khởi tạo Query lấy dữ liệu gốc
            var query = _context.AccountItems
                .Include(a => a.Product)
                .AsNoTracking()
                .AsQueryable();

            // 2. THỐNG KÊ TỔNG (Luôn đếm trên toàn bộ database để hiện số trên nút)
            ViewBag.TotalAvailable = await _context.AccountItems.CountAsync(a => !a.IsSold);
            ViewBag.TotalSold = await _context.AccountItems.CountAsync(a => a.IsSold);
            ViewBag.CurrentStatus = status; // Lưu trạng thái hiện tại để highlight nút

            // 3. LỌC THEO TÌM KIẾM (Nếu có)
            ViewData["CurrentFilter"] = searchString;
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                query = query.Where(s =>
                    (s.Product != null && s.Product.Name.ToLower().Contains(searchString)) ||
                    (s.Username != null && s.Username.ToLower().Contains(searchString)) ||
                    (s.Password != null && s.Password.ToLower().Contains(searchString)) ||
                    (s.Data != null && s.Data.ToLower().Contains(searchString))
                );
            }

            // 4. LỌC THEO NÚT TRẠNG THÁI (Mới thêm)
            if (status == "available")
            {
                query = query.Where(a => !a.IsSold);
            }
            else if (status == "sold")
            {
                query = query.Where(a => a.IsSold);
            }

            // 5. SẮP XẾP VÀ TRẢ VỀ DANH SÁCH
            var list = await query
                .OrderBy(a => a.IsSold)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var accountItem = await _context.AccountItems
                .Include(a => a.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (accountItem == null) return NotFound();

            return View(accountItem);
        }

        // ==========================================
        // THÊM TÀI KHOẢN MỚI
        // ==========================================
        public IActionResult Create()
        {
            if (!_context.Products.Any())
            {
                TempData["Error"] = "Vui lòng tạo ít nhất một Sản phẩm trước khi thêm tài khoản vào kho!";
                return RedirectToAction("Create", "Products");
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Username,Password,Data,IsSold,ProductId")] AccountItem accountItem)
        {
            ModelState.Remove("Product");
            ModelState.Remove("Order");

            if (ModelState.IsValid)
            {
                try
                {
                    accountItem.CreatedAt = DateTime.Now;
                    _context.Add(accountItem);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đã thêm tài khoản vào kho thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", accountItem.ProductId);
            return View(accountItem);
        }

        // ==========================================
        // CHỈNH SỬA TÀI KHOẢN
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var accountItem = await _context.AccountItems.FindAsync(id);
            if (accountItem == null) return NotFound();

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", accountItem.ProductId);
            return View(accountItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,Data,IsSold,ProductId,CreatedAt")] AccountItem accountItem)
        {
            if (id != accountItem.Id) return NotFound();

            ModelState.Remove("Product");
            ModelState.Remove("Order");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(accountItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AccountItemExists(accountItem.Id)) return NotFound();
                    else throw;
                }
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", accountItem.ProductId);
            return View(accountItem);
        }

        // ==========================================
        // XÓA TÀI KHOẢN (Bảo mật: Chỉ Admin mới có quyền Xóa)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var accountItem = await _context.AccountItems
                .Include(a => a.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (accountItem == null) return NotFound();

            return View(accountItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var accountItem = await _context.AccountItems.FindAsync(id);
            if (accountItem != null)
            {
                _context.AccountItems.Remove(accountItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tài khoản khỏi kho!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AccountItemExists(int id)
        {
            return _context.AccountItems.Any(e => e.Id == id);
        }
    }
}