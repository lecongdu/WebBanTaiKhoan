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

        // --- SỬA ĐỔI QUAN TRỌNG TẠI ĐÂY ---
        public async Task<IActionResult> Index()
        {
            // 1. Tìm xem có bản ghi cấu hình nào chưa
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            // 2. Nếu chưa có (Database trắng), tự động tạo mới luôn
            if (settings == null)
            {
                settings = new SystemSetting
                {
                    // Điền dữ liệu giả để không bị lỗi NULL
                    ContactAddress = "Địa chỉ shop (Chưa cập nhật)",
                    ContactEmail = "admin@gmail.com",
                    ContactPhone = "0987654321",
                    BannerUrl = "/images/no-image.png" // Ảnh tạm
                };

                _context.Add(settings);
                await _context.SaveChangesAsync(); // Lưu ngay vào DB
            }

            // 3. Có dữ liệu rồi (hoặc vừa tạo xong), chuyển thẳng sang trang Edit đẹp
            return RedirectToAction(nameof(Edit), new { id = settings.Id });
        }
        // -----------------------------------

        // Trang Create (Vẫn giữ để dự phòng, nhưng thực tế sẽ ít dùng tới)
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemSetting systemSetting, IFormFile? bannerFile1, IFormFile? bannerFile2, IFormFile? bannerFile3)
        {
            if (ModelState.IsValid)
            {
                systemSetting.BannerUrl = await SaveFile(bannerFile1);
                systemSetting.BannerUrl2 = await SaveFile(bannerFile2);
                systemSetting.BannerUrl3 = await SaveFile(bannerFile3);

                _context.Add(systemSetting);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Admin");
            }
            return View(systemSetting);
        }

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
                    // Lấy dữ liệu cũ để giữ lại ảnh nếu không upload mới
                    var existing = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    if (existing == null) return NotFound();

                    // Logic: Có file mới -> Lưu file mới. Không có -> Giữ file cũ
                    systemSetting.BannerUrl = (bannerFile1 != null) ? await SaveFile(bannerFile1) : existing.BannerUrl;
                    systemSetting.BannerUrl2 = (bannerFile2 != null) ? await SaveFile(bannerFile2) : existing.BannerUrl2;
                    systemSetting.BannerUrl3 = (bannerFile3 != null) ? await SaveFile(bannerFile3) : existing.BannerUrl3;

                    _context.Update(systemSetting);
                    await _context.SaveChangesAsync();

                    // Thông báo thành công (tùy chọn)
                    TempData["Success"] = "Đã lưu cấu hình thành công!";
                    return RedirectToAction(nameof(Edit), new { id = systemSetting.Id }); // Ở lại trang Edit để xem kết quả luôn
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }
            return View(systemSetting);
        }

        private async Task<string?> SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            string folder = Path.Combine(_hostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string path = Path.Combine(folder, fileName);
            using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream);
            return "/images/" + fileName;
        }
    }
}