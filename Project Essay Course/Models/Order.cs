using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Essay_Course.Models
{
    public enum OrderStatus
    {
        Pending    = 0,  // Chờ xác nhận
        Confirmed  = 1,  // Đã xác nhận
        Processing = 2,  // Đang chuẩn bị hàng
        Shipping   = 3,  // Đang giao
        Delivered  = 4,  // Hoàn thành
        Cancelled  = 5   // Đã hủy
    }

    public enum PaymentMethod
    {
        COD          = 0,  // Tiền mặt khi nhận
        BankTransfer = 1   // Chuyển khoản
    }

    public enum PaymentStatus
    {
        Unpaid   = 0,
        Paid     = 1,
        Refunded = 2
    }

    // ══════════════════════════════════════════════════════════════
    //  Order
    // ══════════════════════════════════════════════════════════════
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        /// <summary>Mã đơn — user ghi vào nội dung CK. VD: MAISON1234</summary>
        [Required][StringLength(20)]
        public string OrderCode { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        // ── Thông tin nhận hàng ──
        [Required][StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required][StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Email { get; set; }

        [Required][StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required][StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required][StringLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required][StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Note { get; set; }

        // ── Thanh toán ──
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public DateTime? PaidAt { get; set; }

        // ── Trạng thái ──
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // ── Tiền ──
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // ── Timestamps ──
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ──
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // ── Computed ──
        [NotMapped]
        public string StatusDisplay => Status switch
        {
            OrderStatus.Pending    => "Chờ xác nhận",
            OrderStatus.Confirmed  => "Đã xác nhận",
            OrderStatus.Processing => "Đang chuẩn bị hàng",
            OrderStatus.Shipping   => "Đang giao hàng",
            OrderStatus.Delivered  => "Hoàn thành",
            OrderStatus.Cancelled  => "Đã hủy",
            _                      => "Không xác định"
        };

        [NotMapped]
        public string StatusColor => Status switch
        {
            OrderStatus.Pending    => "#f59e0b",
            OrderStatus.Confirmed  => "#3b82f6",
            OrderStatus.Processing => "#8b5cf6",
            OrderStatus.Shipping   => "#0ea5e9",
            OrderStatus.Delivered  => "#16a34a",
            OrderStatus.Cancelled  => "#ef4444",
            _                      => "#999"
        };

        [NotMapped]
        public string PaymentMethodDisplay => PaymentMethod switch
        {
            PaymentMethod.COD          => "Tiền mặt khi nhận (COD)",
            PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
            _                          => "Khác"
        };

        [NotMapped]
        public bool CanCancel => Status == OrderStatus.Pending;
    }

    // ══════════════════════════════════════════════════════════════
    //  OrderItem — snapshot tại thời điểm mua
    // ══════════════════════════════════════════════════════════════
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        // Giữ FK để có thể query, nhưng không bắt buộc (product có thể bị xóa)
        public int? ProductId { get; set; }
        public int? VariantId { get; set; }

        // ── Snapshot — lưu lại thông tin tại thời điểm đặt hàng ──
        [Required][StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ProductSlug { get; set; }

        [StringLength(500)]
        public string? ProductImage { get; set; }

        /// <summary>VD: "Size: M / Màu: Đen"</summary>
        [StringLength(200)]
        public string? VariantDisplay { get; set; }

        [StringLength(100)]
        public string? SKU { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Range(1, 9999)]
        public int Quantity { get; set; }

        // ── Computed ──
        [NotMapped]
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
