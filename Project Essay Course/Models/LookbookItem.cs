using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class LookbookItem
    {
        [Key]
        public int LookbookItemId { get; set; }

        /// <summary>
        /// Thứ tự hiển thị trong lookbook (sort order).
        /// </summary>
        [Display(Name = "Thứ tự")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Ảnh editorial riêng cho item này trong lookbook
        /// (có thể khác ảnh sản phẩm gốc).
        /// </summary>
        [StringLength(500)]
        [Display(Name = "Ảnh lookbook")]
        public string? LookbookImage { get; set; }

        /// <summary>
        /// Caption / chú thích ảnh.
        /// </summary>
        [StringLength(300)]
        [Display(Name = "Caption")]
        public string? Caption { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Foreign keys ─────────────────────────────────────────────
        [Required]
        [Display(Name = "Lookbook")]
        public int LookbookId { get; set; }

        [ForeignKey(nameof(LookbookId))]
        public virtual Lookbook? Lookbook { get; set; }

        [Required]
        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }
    }
}
