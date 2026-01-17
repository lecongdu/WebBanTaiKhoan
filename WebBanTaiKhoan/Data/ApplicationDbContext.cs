using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- CÁC BẢNG DỮ LIỆU CŨ ---
        public DbSet<Product> Products { get; set; }
        public DbSet<AccountItem> AccountItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<TopUpTransaction> TopUpTransactions { get; set; }

        // --- MỚI THÊM: ĐÃ SỬA TÊN THÀNH SystemSettings (Có chữ s) ĐỂ KHỚP VỚI CONTROLLER ---
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; } // Đổi tên thuộc tính ở đây để hết lỗi gạch đỏ ở Controller

        // --- CẤU HÌNH ---
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình số thập phân cho tiền tệ
            builder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<TopUpTransaction>().Property(t => t.Amount).HasColumnType("decimal(18,2)");
        }
        public DbSet<WebBanTaiKhoan.Models.Category> Category { get; set; } = default!;
    }
}