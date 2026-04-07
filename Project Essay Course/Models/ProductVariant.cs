using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    /// <summary>
    /// Biến thể sản phẩm — generic, không hardcode domain.
    ///
    /// Ví dụ sử dụng:
    ///   Quần áo : Attr1Name="Size"      Attr1Value="M"
    ///             Attr2Name="Màu"       Attr2Value="Đen"
    ///
    ///   Bánh kem : Attr1Name="Vị"       Attr1Value="Socola"
    ///              Attr2Name="Kích cỡ"  Attr2Value="16cm"
    ///
    ///   Trang sức: Attr1Name="Chất liệu" Attr1Value="Vàng 18k"
    ///              Attr2Name="Size"      Attr2Value="16"
    ///              Attr3Name="Màu đá"    Attr3Value="Xanh"
    ///
    ///   Đơn giản : chỉ dùng Attr1, để Attr2/Attr3 = null
    /// </summary>
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        // ── Thuộc tính biến thể — tối đa 3 cặp key/value ────────────

        [StringLength(50)]
        [Display(Name = "Thuộc tính 1")]
        public string? Attr1Name { get; set; }     // "Size", "Vị", "Chất liệu"...

        [StringLength(100)]
        [Display(Name = "Giá trị 1")]
        public string? Attr1Value { get; set; }    // "M", "Socola", "Vàng 18k"...

        [StringLength(50)]
        [Display(Name = "Thuộc tính 2")]
        public string? Attr2Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Giá trị 2")]
        public string? Attr2Value { get; set; }

        [StringLength(50)]
        [Display(Name = "Thuộc tính 3")]
        public string? Attr3Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Giá trị 3")]
        public string? Attr3Value { get; set; }

        // ── Tồn kho & giá ───────────────────────────────────────────

        [Required(ErrorMessage = "Số lượng tồn kho không được để trống.")]
        [Range(0, 999999, ErrorMessage = "Số lượng không hợp lệ.")]
        [Display(Name = "Tồn kho")]
        public int StockQty { get; set; } = 0;

        /// <summary>
        /// Giá riêng của biến thể này.
        /// null = lấy BasePrice từ Product.
        /// Dùng khi: size XL đắt hơn S, vàng 18k đắt hơn bạc,...
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999, ErrorMessage = "Giá không hợp lệ.")]
        [Display(Name = "Giá riêng (bỏ trống = dùng giá gốc)")]
        public decimal? PriceOverride { get; set; }

        [StringLength(100)]
        [Display(Name = "Mã SKU biến thể")]
        public string? SKU { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // ── Foreign key ──────────────────────────────────────────────
        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        // ── Computed helpers ─────────────────────────────────────────

        /// <summary>
        /// Tên hiển thị đầy đủ của biến thể.
        /// VD: "Size: M / Màu: Đen" hoặc "Vị: Socola / Kích cỡ: 16cm"
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                var parts = new List<string>();

                // Nếu có Value thì hiện — Name là optional (prefix "Name: Value" hoặc chỉ "Value")
                if (!string.IsNullOrWhiteSpace(Attr1Value))
                    parts.Add(string.IsNullOrWhiteSpace(Attr1Name)
                        ? Attr1Value
                        : $"{Attr1Name}: {Attr1Value}");

                if (!string.IsNullOrWhiteSpace(Attr2Value))
                    parts.Add(string.IsNullOrWhiteSpace(Attr2Name)
                        ? Attr2Value
                        : $"{Attr2Name}: {Attr2Value}");

                if (!string.IsNullOrWhiteSpace(Attr3Value))
                    parts.Add(string.IsNullOrWhiteSpace(Attr3Name)
                        ? Attr3Value
                        : $"{Attr3Name}: {Attr3Value}");

                return string.Join(" / ", parts); // "" nếu không có attr nào
            }
        }

        /// <summary>
        /// Giá thực tế của biến thể.
        /// Ưu tiên PriceOverride, fallback về Product.BasePrice.
        /// </summary>
        [NotMapped]
        public decimal ActualPrice =>
            PriceOverride.HasValue ? PriceOverride.Value : (Product?.BasePrice ?? 0);

        /// <summary>
        /// Còn hàng không?
        /// </summary>
        [NotMapped]
        public bool InStock => IsActive && StockQty > 0;
    }
}