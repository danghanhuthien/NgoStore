using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly identifier, unique.
        /// VD: "ao-linen-dang-rong", "banh-kem-socola-16cm"
        /// </summary>
        [Required]
        [StringLength(200)]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự.")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDesc { get; set; }

        /// <summary>
        /// Giá gốc — luôn bắt buộc.
        /// Nếu có SalePrice thì hiển thị SalePrice, gạch BasePrice.
        /// </summary>
        [Required(ErrorMessage = "Giá sản phẩm không được để trống.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999, ErrorMessage = "Giá không hợp lệ.")]
        [Display(Name = "Giá gốc")]
        public decimal BasePrice { get; set; }

        /// <summary>
        /// Giá sale — null nghĩa là không có sale.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999, ErrorMessage = "Giá sale không hợp lệ.")]
        [Display(Name = "Giá sale")]
        public decimal? SalePrice { get; set; }

        [StringLength(100)]
        [Display(Name = "Mã SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Hiển thị trang chủ")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Foreign key ──────────────────────────────────────────────
        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        // ── Navigation properties ────────────────────────────────────
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // ── Computed helpers (không map vào DB) ──────────────────────

        /// <summary>
        /// True nếu TẤT CẢ variant active đều có PriceOverride.
        /// </summary>
        [NotMapped]
        public bool AllVariantsHavePriceOverride =>
            Variants.Any(v => v.IsActive) &&
            Variants.Where(v => v.IsActive).All(v => v.PriceOverride.HasValue);

        /// <summary>
        /// True nếu nên hiện dạng "Từ X₫" (nhiều mức giá variant khác nhau).
        /// </summary>
        [NotMapped]
        public bool ShowFromPrice =>
            AllVariantsHavePriceOverride &&
            Variants.Where(v => v.IsActive && v.PriceOverride.HasValue)
                    .Select(v => v.PriceOverride!.Value)
                    .Distinct().Count() > 1;

        /// <summary>
        /// Giá hiển thị thực tế trên card/listing:
        /// - Nếu tất cả variant có PriceOverride → lấy giá thấp nhất variant
        /// - Ngược lại → SalePrice nếu có, không thì BasePrice
        /// </summary>
        [NotMapped]
        public decimal DisplayPrice
        {
            get
            {
                if (AllVariantsHavePriceOverride)
                {
                    var minVariantPrice = Variants
                        .Where(v => v.IsActive && v.PriceOverride.HasValue)
                        .Min(v => v.PriceOverride!.Value);
                    return minVariantPrice;
                }
                return SalePrice.HasValue && SalePrice < BasePrice
                    ? SalePrice.Value : BasePrice;
            }
        }

        /// <summary>
        /// Phần trăm giảm giá. Null nếu variant có PriceOverride hoặc không có sale.
        /// </summary>
        [NotMapped]
        public int? DiscountPercent
        {
            get
            {
                if (AllVariantsHavePriceOverride) return null;
                return SalePrice.HasValue && SalePrice < BasePrice
                    ? (int)Math.Round((1 - SalePrice.Value / BasePrice) * 100)
                    : null;
            }
        }

        /// <summary>
        /// Ảnh đại diện — IsMain=true, fallback ảnh đầu tiên nếu không có.
        /// </summary>
        [NotMapped]
        public ProductImage? MainImage =>
            Images.FirstOrDefault(i => i.IsMain) ?? Images.FirstOrDefault();

        /// <summary>
        /// Tổng tồn kho = cộng tất cả variant đang active.
        /// </summary>
        [NotMapped]
        public int TotalStock =>
            Variants.Where(v => v.IsActive).Sum(v => v.StockQty);
    }
}