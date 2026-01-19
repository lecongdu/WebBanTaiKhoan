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
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace WebBanTaiKhoan.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH SẢN PHẨM (Ai cũng xem được)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.AccountItems)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // --- TAO THEM DOAN NAY DE TU DONG DEM SO LUONG ---
            foreach (var item in products)
            {
                item.StockQuantity = item.AccountItems.Count(a => !a.IsSold);
                item.SoldQuantity = item.AccountItems.Count(a => a.IsSold);
            }
            // ------------------------------------------------

            return View(products);
        }

        // CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.AccountItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // --- CAP NHAT SO LUONG TRUOC KHI SANG VIEW ---
            product.StockQuantity = product.AccountItems.Count(a => !a.IsSold);
            product.SoldQuantity = product.AccountItems.Count(a => a.IsSold);

            return View(product);
        }

        // ==========================================
        // 2. CHỨC NĂNG MUA HÀNG (Dành cho thành viên)
        // ==========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id)
        {
            var product = await _context.Products
                .Include(p => p.AccountItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var accountItem = product.AccountItems.FirstOrDefault(a => !a.IsSold);
            if (accountItem == null)
            {
                TempData["Error"] = "Sản phẩm này tạm thời hết hàng!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            if (user.Balance < product.Price)
            {
                TempData["Error"] = $"Số dư không đủ! Bạn còn thiếu {(product.Price - user.Balance):#,##0}đ.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            user.Balance -= product.Price;
            accountItem.IsSold = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Mua hàng thành công! Kiểm tra mục 'Tài khoản của tôi'.";
            TempData["BoughtAccount"] = accountItem.Data;

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // ==========================================
        // 3. KHU VỰC QUẢN TRỊ (ADMIN & CTV)
        // ==========================================

        [Authorize(Roles = "Admin,Collaborator,usert")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "Name");
            return View();
        }

        [Authorize(Roles = "Admin,Collaborator,usert")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,ImageUrl,CategoryId")] Product product, IFormFile? ImageFile)
        {
            ModelState.Remove("Category");
            ModelState.Remove("AccountItems");

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImage(ImageFile);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin,Collaborator,usert")]
        public async Task<IActionResult> ImportStock(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin,Collaborator,usert")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStock(int productId, string listData)
        {
            if (string.IsNullOrWhiteSpace(listData))
            {
                TempData["Error"] = "Vui lòng nhập danh sách tài khoản!";
                return RedirectToAction(nameof(ImportStock), new { id = productId });
            }

            var lines = listData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim())
                                     .Where(l => !string.IsNullOrEmpty(l))
                                     .ToList();

            if (lines.Count > 0)
            {
                var newItems = lines.Select(line => new AccountItem
                {
                    ProductId = productId,
                    Data = line,
                    CreatedAt = DateTime.Now,
                    IsSold = false
                }).ToList();

                _context.AccountItems.AddRange(newItems);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã nhập thành công {newItems.Count} tài khoản vào kho!";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Collaborator,usert")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // --- THEM INCLUDE ACCOUNT ITEMS DE DEM SO LUONG ---
            var product = await _context.Products
                .Include(p => p.AccountItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            product.StockQuantity = product.AccountItems.Count(a => !a.IsSold);
            product.SoldQuantity = product.AccountItems.Count(a => a.IsSold);

            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin,Collaborator,usert")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,ImageUrl,CategoryId")] Product product, IFormFile? ImageFile)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("AccountItems");

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        DeletePhysicalImage(product.ImageUrl);
                        product.ImageUrl = await SaveImage(ImageFile);
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                DeletePhysicalImage(product.ImageUrl);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            string filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/images/" + fileName;
        }

        private void DeletePhysicalImage(string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => id == e.Id);
        }
    }
}