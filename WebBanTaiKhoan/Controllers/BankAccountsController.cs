using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    public class BankAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public BankAccountsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. Danh sách
        public async Task<IActionResult> Index()
        {
            return View(await _context.BankAccounts.ToListAsync());
        }

        // 2. Tạo mới (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. Tạo mới (POST) - Có xử lý upload ảnh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankAccount bankAccount, IFormFile? logoFile, IFormFile? qrFile)
        {
            // Xử lý Logo
            if (logoFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                string uploadPath = Path.Combine(_hostEnvironment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                bankAccount.LogoUrl = "/images/" + fileName;
            }
            else bankAccount.LogoUrl = "";

            // Xử lý QR
            if (qrFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(qrFile.FileName);
                string uploadPath = Path.Combine(_hostEnvironment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await qrFile.CopyToAsync(stream);
                }
                bankAccount.QrUrl = "/images/" + fileName;
            }
            else bankAccount.QrUrl = "";

            // Bỏ qua validate 2 trường này vì đã xử lý tay
            ModelState.Remove("LogoUrl");
            ModelState.Remove("QrUrl");

            if (ModelState.IsValid)
            {
                _context.Add(bankAccount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bankAccount);
        }

        // 4. Sửa (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount == null) return NotFound();
            return View(bankAccount);
        }

        // 5. Sửa (POST) - Có xử lý thay ảnh mới hoặc giữ ảnh cũ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BankAccount bankAccount, IFormFile? logoFile, IFormFile? qrFile)
        {
            if (id != bankAccount.Id) return NotFound();

            // Lấy thông tin cũ để giữ lại ảnh nếu người dùng không chọn ảnh mới
            var existingAccount = await _context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (existingAccount == null) return NotFound();

            // Xử lý Logo
            if (logoFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                string uploadPath = Path.Combine(_hostEnvironment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                bankAccount.LogoUrl = "/images/" + fileName;
            }
            else
            {
                bankAccount.LogoUrl = existingAccount.LogoUrl; // Giữ nguyên
            }

            // Xử lý QR
            if (qrFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(qrFile.FileName);
                string uploadPath = Path.Combine(_hostEnvironment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await qrFile.CopyToAsync(stream);
                }
                bankAccount.QrUrl = "/images/" + fileName;
            }
            else
            {
                bankAccount.QrUrl = existingAccount.QrUrl; // Giữ nguyên
            }

            ModelState.Remove("LogoUrl");
            ModelState.Remove("QrUrl");

            if (ModelState.IsValid)
            {
                _context.Update(bankAccount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bankAccount);
        }

        // 6. Xóa (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var bankAccount = await _context.BankAccounts.FirstOrDefaultAsync(m => m.Id == id);
            if (bankAccount == null) return NotFound();
            return View(bankAccount);
        }

        // 7. Xóa (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount != null)
            {
                _context.BankAccounts.Remove(bankAccount);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}