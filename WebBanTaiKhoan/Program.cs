using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanTaiKhoan;
using WebBanTaiKhoan.Data;
using WebBanTaiKhoan.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CẤU HÌNH DATABASE ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. CẤU HÌNH IDENTITY (SỬ DỤNG APPLICATIONUSER) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Tắt các ràng buộc khắt khe để dễ test
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Cấu hình Password đơn giản
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI()
.AddErrorDescriber<VietnameseIdentityErrorDescriber>(); // <--- THÊM DÒNG NÀY ĐỂ TIẾNG VIỆT HÓA LỖI

// Cấu hình Cookie để đảm bảo chuyển hướng đúng khi chưa đăng nhập
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// --- 3. CẤU HÌNH SESSION ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ==================================================================
// --- BẮT ĐẦU: SEED DATA (TỰ ĐỘNG TẠO DỮ LIỆU) ---
// ==================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Tạo các Role mặc định
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Tạo tài khoản Admin mẫu
        string adminEmail = "admin@gmail.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Quản trị viên",
                Balance = 5000000 // Tặng 5 triệu vào ví Admin
            };

            var createAdmin = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Tạo Danh mục mẫu
        if (!context.Category.Any())
        {
            context.Category.AddRange(
                new Category { Name = "Netflix Premium", Description = "Tài khoản xem phim 4K" },
                new Category { Name = "Spotify Premium", Description = "Nghe nhạc chất lượng cao" },
                new Category { Name = "Youtube Premium", Description = "Xem video không quảng cáo" }
            );
            await context.SaveChangesAsync();
        }

        // 4. Tạo Sản phẩm mẫu
        if (!context.Products.Any())
        {
            var catNetflix = await context.Category.FirstOrDefaultAsync(c => c.Name == "Netflix Premium");
            if (catNetflix != null)
            {
                context.Products.Add(new Product
                {
                    Name = "Netflix 1 Tháng (Chính chủ)",
                    Price = 260000,
                    Description = "Bảo hành trọn thời gian sử dụng.",
                    CategoryId = catNetflix.Id,
                    ImageUrl = "https://img.freepik.com/free-vector/netflix-logo-black-background_1017-25164.jpg"
                });
                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi Seed Data.");
    }
}

// --- 4. CẤU HÌNH MIDDLEWARE ---
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();