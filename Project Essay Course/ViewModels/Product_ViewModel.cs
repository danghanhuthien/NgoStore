using System.ComponentModel.DataAnnotations;
using Project_Essay_Course.Models;

namespace Project_Essay_Course.ViewModels.Product_ViewModel
{
    // ══════════════════════════════════════════════════════════════════
    //  INPUT: Variant row (dùng chung Create + Edit)
    // ══════════════════════════════════════════════════════════════════
    public class VariantInputModel
    {
        public int VariantId { get; set; }  // 0 = mới, > 0 = cập nhật

        [StringLength(50)]
        [Display(Name = "Tên thuộc tính 1")]
        public string? Attr1Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Giá trị 1")]
        public string? Attr1Value { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên thuộc tính 2")]
        public string? Attr2Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Giá trị 2")]
        public string? Attr2Value { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên thuộc tính 3")]
        public string? Attr3Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Giá trị 3")]
        public string? Attr3Value { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Tồn kho")]
        public int StockQty { get; set; } = 0;

        [Display(Name = "Giá riêng")]
        public decimal? PriceOverride { get; set; }

        [StringLength(100)]
        [Display(Name = "SKU biến thể")]
        public string? SKU { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════
    //  CREATE
    // ══════════════════════════════════════════════════════════════════
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(200)]
        [RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDesc { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Range(0, 999999999)]
        [Display(Name = "Giá gốc (₫)")]
        public decimal BasePrice { get; set; }

        [Range(0, 999999999)]
        [Display(Name = "Giá sale (₫) — bỏ trống nếu không có")]
        public decimal? SalePrice { get; set; }

        [StringLength(100)]
        [Display(Name = "Mã SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Hiển thị trang chủ")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        // Upload ảnh — nhiều file
        [Display(Name = "Hình ảnh sản phẩm")]
        public List<IFormFile>? Images { get; set; }

        // Variants — danh sách động
        public List<VariantInputModel>? Variants { get; set; }

        // Tồn kho mặc định khi không có variant
        [Range(0, 999999)]
        [Display(Name = "Tồn kho mặc định")]
        public int DefaultStock { get; set; } = 0;
    }

    // ══════════════════════════════════════════════════════════════════
    //  EDIT
    // ══════════════════════════════════════════════════════════════════
    public class ProductEditViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug không được để trống.")]
        [StringLength(200)]
        [RegularExpression(@"^[a-z0-9\-]+$",
            ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDesc { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Range(0, 999999999)]
        [Display(Name = "Giá gốc (₫)")]
        public decimal BasePrice { get; set; }

        [Range(0, 999999999)]
        [Display(Name = "Giá sale (₫)")]
        public decimal? SalePrice { get; set; }

        [StringLength(100)]
        [Display(Name = "Mã SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Hiển thị trang chủ")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        // Ảnh đang có (hiển thị để xóa)
        public List<ProductImage> ExistingImages { get; set; } = new();

        // ID ảnh muốn xóa
        public List<int>? DeleteImageIds { get; set; }

        // Upload ảnh mới
        [Display(Name = "Thêm ảnh mới")]
        public List<IFormFile>? NewImages { get; set; }

        // Variants
        public List<VariantInputModel> Variants { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════════
    //  PUBLIC LIST (trang shop)
    // ══════════════════════════════════════════════════════════════════
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Filter state
        public int?     CurrentCategoryId { get; set; }
        public string?  Search    { get; set; }
        public decimal? MinPrice  { get; set; }
        public decimal? MaxPrice  { get; set; }
        public string?  Sort      { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages  { get; set; }
        public int TotalItems  { get; set; }

        public bool HasPrev => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    // ══════════════════════════════════════════════════════════════════
    //  PUBLIC DETAIL (trang chi tiết sản phẩm)
    // ══════════════════════════════════════════════════════════════════
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> Related { get; set; } = new();
    }
}
