using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Models;
using Project_Essay_Course.Services;
using Project_Essay_Course.ViewModels.Order_ViewModel;

namespace Project_Essay_Course.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderAdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public OrderAdminController(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /OrderAdmin — Danh sách đơn hàng
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? search, OrderStatus? status, int page = 1)
        {
            const int pageSize = 15;

            var query = _db.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderCode.ToLower().Contains(search) ||
                    o.FullName.ToLower().Contains(search) ||
                    o.Phone.Contains(search));
            }

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new OrderAdminListViewModel
            {
                Orders = orders,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = total,
                Search = search,
                StatusFilter = status,
                PendingCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                ConfirmedCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed),
                ShippingCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Shipping),
                DeliveredCount = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Delivered)
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /OrderAdmin/Detail/{id}
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /OrderAdmin/UpdateStatus — Cập nhật trạng thái đơn
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus newStatus)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            // Không cho phép quay lui trạng thái (trừ Cancel)
            if (newStatus != OrderStatus.Cancelled && (int)newStatus < (int)order.Status)
            {
                TempData["Error"] = "Không thể quay lại trạng thái trước.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            // Nếu hủy → hoàn tồn kho
            if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _db.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null) variant.StockQty += item.Quantity;
                    }
                }
            }

            // Nếu Delivered + COD → tự động đánh dấu Paid
            if (newStatus == OrderStatus.Delivered && order.PaymentMethod == PaymentMethod.COD)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Gửi email thông báo cho khách (fire-and-forget)
            var statusesToNotify = new[] {
                OrderStatus.Confirmed, OrderStatus.Processing,
                OrderStatus.Shipping,  OrderStatus.Delivered, OrderStatus.Cancelled
            };
            if (statusesToNotify.Contains(newStatus))
            {
                _ = Task.Run(() => _emailService.SendOrderStatusUpdateAsync(order));
            }

            TempData["Success"] = $"Đã cập nhật đơn {order.OrderCode} → {order.StatusDisplay}";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /OrderAdmin/UpdatePaymentStatus — Cập nhật thanh toán
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int orderId, bool isPaid)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.PaymentStatus = isPaid ? PaymentStatus.Paid : PaymentStatus.Unpaid;
            order.PaidAt = isPaid ? DateTime.UtcNow : null;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = isPaid
                ? $"Đã xác nhận thanh toán cho đơn {order.OrderCode}."
                : $"Đã đánh dấu chưa thanh toán cho đơn {order.OrderCode}.";

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /OrderAdmin/Invoice/{id} — Xuất hóa đơn
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View("Invoice", order);
        }
    }
}