using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Models;
using Project_Essay_Course.ViewModels.Order_ViewModel;

namespace Project_Essay_Course.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public OrderController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Order — Lịch sử đơn hàng
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            const int pageSize = 8;
            var userId = _userManager.GetUserId(User)!;

            var query = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            if (Enum.TryParse<OrderStatus>(status, out var statusEnum))
                query = query.Where(o => o.Status == statusEnum);

            var total      = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var orders     = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new OrderListViewModel
            {
                Orders       = orders,
                CurrentPage  = page,
                TotalPages   = totalPages,
                StatusFilter = status
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Order/Detail/{id}
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order  = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View(order);
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Order/Cancel/{id} — Hủy đơn (chỉ khi Pending)
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order  = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();

            if (!order.CanCancel)
            {
                TempData["Error"] = "Không thể hủy đơn hàng này.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.Status    = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            // Hoàn lại tồn kho
            foreach (var item in order.OrderItems)
            {
                if (item.VariantId.HasValue)
                {
                    var variant = await _db.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null) variant.StockQty += item.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã hủy đơn hàng {order.OrderCode}.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Order/Track — Theo dõi đơn (không cần login)
        // ══════════════════════════════════════════════════════════════
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Track(string? code)
        {
            Order? order = null;
            if (!string.IsNullOrEmpty(code))
            {
                order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderCode == code.Trim().ToUpper());

                if (order == null)
                    TempData["Error"] = "Không tìm thấy đơn hàng với mã này.";
            }

            ViewBag.SearchCode = code;
            return View(order);
        }
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View("Invoice", order);
        }
    }
}
