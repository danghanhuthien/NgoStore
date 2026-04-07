using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        // ── Owner ────────────────────────────────────────────────────
        [Required]
        public string UserId { get; set; } = string.Empty;

        // ── Product ──────────────────────────────────────────────────
        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;

        // ── Variant (nullable — sản phẩm không có variant thì null) ──
        public int? VariantId { get; set; }

        [ForeignKey(nameof(VariantId))]
        public virtual ProductVariant? Variant { get; set; }

        // ── Quantity ─────────────────────────────────────────────────
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        // ── Timestamp ────────────────────────────────────────────────
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // ── Computed helpers ─────────────────────────────────────────

        /// <summary>
        /// Giá đơn vị thực tế:
        /// PriceOverride của variant → SalePrice → BasePrice
        /// </summary>
        [NotMapped]
        public decimal UnitPrice
        {
            get
            {
                if (Variant?.PriceOverride.HasValue == true)
                    return Variant.PriceOverride.Value;
                if (Product?.SalePrice.HasValue == true && Product.SalePrice < Product.BasePrice)
                    return Product.SalePrice.Value;
                return Product?.BasePrice ?? 0;
            }
        }

        /// <summary>Thành tiền = đơn giá × số lượng</summary>
        [NotMapped]
        public decimal SubTotal => UnitPrice * Quantity;

        /// <summary>
        /// Tên hiển thị biến thể, VD: "Size: M / Màu: Đen".
        /// Trả về "" nếu variant là default (không có attr).
        /// </summary>
        [NotMapped]
        public string VariantDisplay
        {
            get
            {
                if (Variant == null) return "";
                // Variant default = không có attr nào → không hiển thị
                var hasAttr = !string.IsNullOrWhiteSpace(Variant.Attr1Value)
                           || !string.IsNullOrWhiteSpace(Variant.Attr2Value)
                           || !string.IsNullOrWhiteSpace(Variant.Attr3Value);
                return hasAttr ? Variant.DisplayName : "";
            }
        }
    }
} 