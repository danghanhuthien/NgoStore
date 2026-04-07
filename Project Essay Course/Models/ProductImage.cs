using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        /// <summary>
        /// Đường dẫn tương đối từ wwwroot.
        /// VD: "/uploads/products/3f2a1b4c-guid.jpg"
        /// Khi render: &lt;img src="@image.ImagePath" /&gt;
        /// Không lưu full URL để dễ migrate server/cloud sau này.
        /// </summary>
        [Required]
        [StringLength(500)]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Mô tả ảnh (alt text)")]
        public string? AltText { get; set; }

        /// <summary>
        /// Ảnh đại diện — mỗi sản phẩm chỉ có 1 ảnh IsMain=true.
        /// Controller cần đảm bảo điều này khi save.
        /// </summary>
        [Display(Name = "Ảnh đại diện")]
        public bool IsMain { get; set; } = false;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        // ── Foreign key ──────────────────────────────────────────────
        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }
    }
}
