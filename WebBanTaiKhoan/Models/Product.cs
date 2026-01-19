using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanTaiKhoan.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá bán")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")] // Đảm bảo độ chính xác của tiền tệ trong SQL
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        // === TAO THEM 2 DONG NAY DE HIEN THI SO LUONG ===
        [NotMapped] // Dung NotMapped neu may muon tu dem trong code ma khong can tao cot SQL
        [Display(Name = "Còn hàng")]
        public int StockQuantity { get; set; }

        [NotMapped] // Dung NotMapped neu may muon tu dem trong code ma khong can tao cot SQL
        [Display(Name = "Đã bán")]
        public int SoldQuantity { get; set; }
        // ================================================

        // ==========================================
        // QUẢN LÝ DANH MỤC (ADMIN CÓ THỂ ĐIỀU CHỈNH)
        // ==========================================

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; } // Cho phép null để tránh lỗi khi chưa phân loại

        [ForeignKey("CategoryId")]
        [Display(Name = "Tên danh mục")]
        public virtual Category? Category { get; set; }

        // ==========================================
        // QUAN HỆ VỚI KHO HÀNG (ACCOUNT ITEMS)
        // ==========================================

        public virtual ICollection<AccountItem> AccountItems { get; set; } = new List<AccountItem>();
    }
}