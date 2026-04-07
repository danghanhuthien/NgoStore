using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class Lookbook
    {
        [Key]
        public int LookbookId { get; set; }

        [Required(ErrorMessage = "Tiêu đề lookbook không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly identifier, unique.
        /// VD: "summer-collection-2025", "ao-linen-mua-he"
        /// </summary>
        [Required]
        [StringLength(200)]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự.")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDesc { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        /// <summary>
        /// Ảnh bìa của lookbook (hero image).
        /// </summary>
        [StringLength(500)]
        [Display(Name = "Ảnh bìa")]
        public string? CoverImage { get; set; }

        /// <summary>
        /// Season / Collection tag. VD: "Summer 2025", "Fall/Winter"
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Bộ sưu tập")]
        public string? Season { get; set; }

        [Display(Name = "Hiển thị trang chủ")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation properties ────────────────────────────────────
        public virtual ICollection<LookbookItem> Items { get; set; } = new List<LookbookItem>();

        // ── Computed helpers (không map vào DB) ──────────────────────

        /// <summary>
        /// Số lượng sản phẩm trong lookbook.
        /// </summary>
        [NotMapped]
        public int ItemCount => Items.Count;
    }
}
