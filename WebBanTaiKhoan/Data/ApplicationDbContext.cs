using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Models;

namespace WebBanTaiKhoan.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- CÁC BẢNG DỮ LIỆU ---
        public DbSet<Product> Products { get; set; }
        public DbSet<AccountItem> AccountItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<TopUpTransaction> TopUpTransactions { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Category> Category { get; set; }

        // --- THÊM BẢNG GIỎ HÀNG Ở ĐÂY ---
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<TopUpTransaction>().Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Entity<ApplicationUser>().Property(u => u.Balance).HasColumnType("decimal(18,2)");

            // Cấu hình bảng CartItem (Số tiền trong giỏ)
            builder.Entity<CartItem>().Property(c => c.Price).HasColumnType("decimal(18,2)");
        }
    }
}