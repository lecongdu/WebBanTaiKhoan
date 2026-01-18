using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BankAccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public BankAccountsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. DANH SÁCH
        public async Task<IActionResult> Index()
        {
            return View(await _context.BankAccounts.ToListAsync());
        }

        // ==========================================
        // 2. CHI TIẾT (HÀM NÀY BỊ THIẾU DẪN ĐẾN LỖI 404)
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bankAccount == null) return NotFound();

            return View(bankAccount);
        }

        // 3. TẠO MỚI (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 4. TẠO MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankAccount bankAccount, IFormFile? logoFile)
        {
            ModelState.Remove("LogoUrl");
            ModelState.Remove("QrUrl");

            if (ModelState.IsValid)
            {
                if (logoFile != null)
                    bankAccount.LogoUrl = await SaveImage(logoFile, "logos");

                // Vì dùng QR động nên QrUrl có thể để trống hoặc gán chuỗi mặc định
                bankAccount.QrUrl = "";

                _context.Add(bankAccount);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm ngân hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(bankAccount);
        }

        // 5. SỬA (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount == null) return NotFound();
            return View(bankAccount);
        }

        // 6. SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BankAccount bankAccount, IFormFile? logoFile)
        {
            if (id != bankAccount.Id) return NotFound();

            var oldBank = await _context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (oldBank == null) return NotFound();

            ModelState.Remove("LogoUrl");
            ModelState.Remove("QrUrl");

            if (ModelState.IsValid)
            {
                if (logoFile != null)
                {
                    DeleteImage(oldBank.LogoUrl);
                    bankAccount.LogoUrl = await SaveImage(logoFile, "logos");
                }
                else
                {
                    bankAccount.LogoUrl = oldBank.LogoUrl;
                }

                bankAccount.QrUrl = oldBank.QrUrl; // Giữ nguyên giá trị cũ

                _context.Update(bankAccount);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(bankAccount);
        }

        // 7. XÓA (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var bankAccount = await _context.BankAccounts.FirstOrDefaultAsync(m => m.Id == id);
            if (bankAccount == null) return NotFound();
            return View(bankAccount);
        }

        // 8. XÓA (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount != null)
            {
                DeleteImage(bankAccount.LogoUrl);
                _context.BankAccounts.Remove(bankAccount);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa ngân hàng!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile file, string subFolder)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", subFolder);
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            string filePath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/images/{subFolder}/{fileName}";
        }

        private void DeleteImage(string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
            {
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, imageUrl.TrimStart('/').Replace("/", "\\"));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }
}