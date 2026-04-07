using System.ComponentModel.DataAnnotations;
using Project_Essay_Course.Models;

namespace Project_Essay_Course.ViewModels.Order_ViewModel
{
    // ══════════════════════════════════════════════════════════════
    //  Checkout form
    // ══════════════════════════════════════════════════════════════
    public class CheckoutViewModel
    {
        // ── Thông tin nhận hàng ──
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0|\+84)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        [Display(Name = "Tỉnh / Thành phố")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn quận/huyện.")]
        [Display(Name = "Quận / Huyện")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        [Display(Name = "Phường / Xã")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể.")]
        [StringLength(300)]
        [Display(Name = "Số nhà, tên đường")]
        public string Address { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // ── Thanh toán ──
        [Required]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        // ── Cart summary (readonly, không submit) ──
        public List<CartItemSummary> CartItems { get; set; } = new();
        public decimal SubTotal    { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CartItemSummary
    {
        public string ProductName    { get; set; } = string.Empty;
        public string? ProductImage  { get; set; }
        public string? VariantDisplay { get; set; }
        public decimal UnitPrice     { get; set; }
        public int Quantity          { get; set; }
        public decimal SubTotal      => UnitPrice * Quantity;
    }

    // ══════════════════════════════════════════════════════════════
    //  Order list (user)
    // ══════════════════════════════════════════════════════════════
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages  { get; set; } = 1;
        public string? StatusFilter { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  Order Admin list
    // ══════════════════════════════════════════════════════════════
    public class OrderAdminListViewModel
    {
        public List<Order> Orders    { get; set; } = new();
        public int CurrentPage       { get; set; } = 1;
        public int TotalPages        { get; set; } = 1;
        public int TotalItems        { get; set; }
        public string? Search        { get; set; }
        public OrderStatus? StatusFilter { get; set; }
        // Stats
        public int PendingCount    { get; set; }
        public int ConfirmedCount  { get; set; }
        public int ShippingCount   { get; set; }
        public int DeliveredCount  { get; set; }
    }
}
