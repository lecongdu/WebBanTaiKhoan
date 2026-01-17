using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanTaiKhoan.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // Danh sách sản phẩm thuộc danh mục này
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}