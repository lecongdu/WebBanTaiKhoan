using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;

namespace WebBanTaiKhoan.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. TÍNH TOÁN CÁC CHỈ SỐ TỔNG QUÁT
            ViewBag.Revenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.Orders = await _context.Orders.CountAsync();
            ViewBag.Products = await _context.Products.CountAsync();
            ViewBag.Accounts = await _context.AccountItems.CountAsync(a => !a.IsSold);

            // 2. LẤY DỮ LIỆU DOANH THU 7 NGÀY GẦN NHẤT CHO BIỂU ĐỒ
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-6);

            // Lấy danh sách 7 ngày gần đây
            var dateList = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i))
                .ToList();

            // Truy vấn doanh thu theo từng ngày từ Database
            var dailyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            // Khớp dữ liệu: Nếu ngày nào không có đơn hàng thì gán giá trị bằng 0
            var chartLabels = dateList.Select(d => d.ToString("dd/MM")).ToList();
            var chartValues = dateList.Select(d => dailyRevenue.FirstOrDefault(r => r.Date == d)?.Total ?? 0).ToList();

            // Gửi dữ liệu biểu đồ sang View
            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartValues = chartValues;

            return View();
        }
    }
}