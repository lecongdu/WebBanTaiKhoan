using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccountItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // DANH SÁCH TÀI KHOẢN - CÓ THÊM TÌM KIẾM
        // ==========================================
        public async Task<IActionResult> Index(string searchString)
        {
            // Bắt đầu truy vấn bao gồm thông tin sản phẩm
            var query = _context.AccountItems
                .Include(a => a.Product)
                .AsNoTracking() // Tăng tốc độ load dữ liệu
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm (Tên tài khoản hoặc Tên sản phẩm)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Username.Contains(searchString)
                                      || s.Product.Name.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }

            // Sắp xếp: Ưu tiên tài khoản CHƯA BÁN lên đầu, sau đó theo ID mới nhất
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
        public async Task<IActionResult> Create([Bind("Id,Username,Password,IsSold,ProductId")] AccountItem accountItem)
        {
            // Xóa kiểm tra Product để tránh lỗi ModelState không hợp lệ
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,IsSold,ProductId")] AccountItem accountItem)
        {
            if (id != accountItem.Id) return NotFound();

            ModelState.Remove("Product");

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
        // XÓA TÀI KHOẢN
        // ==========================================
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