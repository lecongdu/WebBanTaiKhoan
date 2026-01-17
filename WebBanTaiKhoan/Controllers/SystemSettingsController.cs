using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Controllers
{
    public class SystemSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public SystemSettingsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. Tự động điều hướng: Nếu có dữ liệu rồi thì vào Edit, chưa có thì vào Create
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings == null) return RedirectToAction(nameof(Create));
            return RedirectToAction(nameof(Edit), new { id = settings.Id });
        }

        // 2. Tạo mới cấu hình (Chỉ dùng cho lần đầu tiên)
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemSetting systemSetting, IFormFile? bannerFile1, IFormFile? bannerFile2, IFormFile? bannerFile3)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    systemSetting.BannerUrl = await SaveFile(bannerFile1);
                    systemSetting.BannerUrl2 = await SaveFile(bannerFile2);
                    systemSetting.BannerUrl3 = await SaveFile(bannerFile3);

                    _context.Add(systemSetting);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Khởi tạo cấu hình thành công!";
                    return RedirectToAction("Index", "Admin");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi tạo mới: " + ex.Message);
                }
            }
            return View(systemSetting);
        }

        // 3. Chỉnh sửa cấu hình
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var systemSetting = await _context.SystemSettings.FindAsync(id);
            if (systemSetting == null) return NotFound();
            return View(systemSetting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SystemSetting systemSetting, IFormFile? bannerFile1, IFormFile? bannerFile2, IFormFile? bannerFile3)
        {
            if (id != systemSetting.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy dữ liệu hiện tại từ DB để so sánh
                    var existingSetting = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    if (existingSetting == null) return NotFound();

                    // Xử lý logic ảnh: Nếu có file mới thì upload, không thì lấy lại Url cũ từ DB
                    systemSetting.BannerUrl = (bannerFile1 != null) ? await SaveFile(bannerFile1) : existingSetting.BannerUrl;
                    systemSetting.BannerUrl2 = (bannerFile2 != null) ? await SaveFile(bannerFile2) : existingSetting.BannerUrl2;
                    systemSetting.BannerUrl3 = (bannerFile3 != null) ? await SaveFile(bannerFile3) : existingSetting.BannerUrl3;

                    _context.Update(systemSetting);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật cấu hình hệ thống thành công!";
                    return RedirectToAction("Index", "Admin");
                }
                catch (DbUpdateException dbEx)
                {
                    var message = dbEx.InnerException?.Message ?? dbEx.Message;
                    ModelState.AddModelError("", "Lỗi Database (Kiểm tra các trường bắt buộc): " + message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            return View(systemSetting);
        }

        // 4. Hàm phụ xử lý lưu File
        private async Task<string?> SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            try
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string uploadPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return "/images/" + fileName;
            }
            catch
            {
                return null; // Trả về null nếu quá trình lưu file thất bại
            }
        }
    }
}