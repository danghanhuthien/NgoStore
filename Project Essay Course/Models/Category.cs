using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Đường dẫn ảnh")]
        public string? ImagePath { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // ── Self-referencing: sub-category ──────────────────────────
        // null = đây là category gốc (root)
        // có giá trị = đây là sub-category
        [Display(Name = "Danh mục cha")]
        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public virtual Category? Parent { get; set; }

        // ── Navigation properties ────────────────────────────────────
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
