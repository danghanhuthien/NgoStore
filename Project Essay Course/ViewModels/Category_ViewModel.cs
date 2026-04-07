using System.ComponentModel.DataAnnotations;

namespace Project_Essay_Course.ViewModels.Category_ViewModel
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tối đa 100 ký tự.")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Danh mục cha")]
        public int? ParentId { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ảnh danh mục")]
        public IFormFile? ImageFile { get; set; }
    }

    public class CategoryEditViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Danh mục cha")]
        public int? ParentId { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // Ảnh hiện tại
        public string? ExistingImagePath { get; set; }

        [Display(Name = "Ảnh mới")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Xóa ảnh hiện tại")]
        public bool RemoveImage { get; set; } = false;
    }
}
