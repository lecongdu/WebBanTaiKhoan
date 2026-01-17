using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        // --- CẤU HÌNH GIAO DIỆN TRANG CHỦ ---
        public string? BannerUrl { get; set; }
        public string? BannerUrl2 { get; set; }
        public string? BannerUrl3 { get; set; }
        public string? MarqueeText { get; set; }

        // --- THÔNG TIN LIÊN HỆ (HEADER & FOOTER) ---
        public string? ContactZalo { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactAddress { get; set; }
        public string? MapHtml { get; set; }

        // --- CẤU HÌNH FOOTER ĐỘNG (MỚI THÊM) ---
        public string? FooterAbout { get; set; }   // Giới thiệu ngắn ở chân trang
        public string? FacebookUrl { get; set; }   // Link Fanpage
        public string? YoutubeUrl { get; set; }    // Link Youtube
        public string? TiktokUrl { get; set; }     // Link Tiktok
        public string? DiscordUrl { get; set; }    // Link Discord
        public string? DmcaUrl { get; set; }       // Link chứng nhận DMCA (nếu có)
    }
}