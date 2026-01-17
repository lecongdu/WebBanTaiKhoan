using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CẤU HÌNH DATABASE ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. CẤU HÌNH IDENTITY (Quản lý Tài khoản & Phân quyền) ---
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    // Tắt các ràng buộc khắt khe để bạn dễ dàng test và đăng nhập
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Đơn giản hóa mật khẩu
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>() // Cực kỳ quan trọng để dùng được [Authorize(Roles = "Admin")]
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Đảm bảo Identity Pages hoạt động

// --- 3. CẤU HÌNH SESSION (Dùng cho Giỏ hàng ShopCart) ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- 4. CẤU HÌNH MIDDLEWARE (Thứ tự là sống còn) ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// KÍCH HOẠT SESSION (Phải nằm sau UseRouting và trước UseAuthorization)
app.UseSession();

app.UseAuthentication(); // Ai đang truy cập?
app.UseAuthorization();  // Người đó có quyền làm gì?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();