// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebBanTaiKhoan.Models;
using Microsoft.AspNetCore.Http; // Thêm cái này để dùng được Session

namespace WebBanTaiKhoan.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // 1. Thực hiện đăng xuất tài khoản Identity
            await _signInManager.SignOutAsync();

            // 🔥 2. XÓA GIỎ HÀNG TRONG SESSION (Quan trọng nhất)
            // Lệnh này đảm bảo khi người khác dùng máy này sẽ không thấy giỏ hàng cũ nữa.
            HttpContext.Session.Remove("ShopCart");

            // Hoặc nếu muốn xóa sạch mọi thứ trong session thì dùng:
            // HttpContext.Session.Clear();

            _logger.LogInformation("Người dùng đã đăng xuất và xóa giỏ hàng Session.");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // Trở về trang chủ sau khi thoát
                return RedirectToPage("/Index");
            }
        }
    }
}