using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        // --- CẤU HÌNH GIAO DIỆN TRANG CHỦ ---
        public string? BannerUrl { get; set; }   // Banner chính
        public string? BannerUrl2 { get; set; }  // Banner phụ 1
        public string? BannerUrl3 { get; set; }  // Banner phụ 2
        public string? MarqueeText { get; set; } // Chữ chạy

        // --- 🟢 CẤU HÌNH TRANG CHÀO (WELCOME PAGE) ---
        public string? WelcomeBadge { get; set; }      // Dòng chữ nhỏ trên đầu (Vd: Nick3s - Uy Tín...)
        public string? WelcomeTitle { get; set; }      // Tiêu đề chính (Vd: KHO TÀI KHOẢN SỐ...)
        public string? WelcomeSubTitle { get; set; }   // Mô tả ngắn bên dưới
        public string? WelcomeButtonText { get; set; } // Chữ trên nút bấm vào shop

        // --- THÔNG TIN LIÊN HỆ ---
        public string? ContactZalo { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactAddress { get; set; }
        public string? MapHtml { get; set; } // Mã nhúng Google Map

        // --- CẤU HÌNH FOOTER & MẠNG XÃ HỘI ---
        public string? FooterAbout { get; set; }
        public string? FacebookUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? TiktokUrl { get; set; }
        public string? DiscordUrl { get; set; }
        public string? DmcaUrl { get; set; }
    }
}