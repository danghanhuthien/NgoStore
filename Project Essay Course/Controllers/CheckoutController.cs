using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Models;
using Project_Essay_Course.Services;
using Project_Essay_Course.ViewModels.Order_ViewModel;

namespace Project_Essay_Course.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager, IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Checkout — Form checkout
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var cartItems = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p.Images)
                .Include(c => c.Product).ThenInclude(p => p.Category)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedAt)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống, vui lòng thêm sản phẩm trước.";
                return RedirectToAction("Index", "Cart");
            }

            var subTotal = cartItems.Sum(c => c.SubTotal);
            var shippingFee = subTotal >= 500000 ? 0 : 30000;
            var total = subTotal + shippingFee;

            // Pre-fill email từ account
            var user = await _userManager.GetUserAsync(User);

            var vm = new CheckoutViewModel
            {
                Email = user?.Email,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = total,
                CartItems = cartItems.Select(c => new CartItemSummary
                {
                    ProductName = c.Product.Name,
                    ProductImage = c.Product.MainImage?.ImagePath,
                    VariantDisplay = c.VariantDisplay,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity
                }).ToList()
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Checkout — Tạo đơn hàng
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;

            // Reload cart từ DB (không tin vào form)
            var cartItems = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p.Images)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Recalculate server-side
            var subTotal = cartItems.Sum(c => c.SubTotal);
            var shippingFee = subTotal >= 500000 ? 0m : 30000m;
            var total = subTotal + shippingFee;

            if (!ModelState.IsValid)
            {
                vm.SubTotal = subTotal;
                vm.ShippingFee = shippingFee;
                vm.TotalAmount = total;
                vm.CartItems = cartItems.Select(c => new CartItemSummary
                {
                    ProductName = c.Product.Name,
                    ProductImage = c.Product.MainImage?.ImagePath,
                    VariantDisplay = c.VariantDisplay,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity
                }).ToList();
                return View(vm);
            }

            // Kiểm tra tồn kho lần cuối
            foreach (var item in cartItems)
            {
                var stock = item.Variant?.StockQty ?? item.Product.TotalStock;
                if (item.Quantity > stock)
                {
                    ModelState.AddModelError("",
                        $"Sản phẩm \"{item.Product.Name}\" chỉ còn {stock} trong kho.");
                    vm.SubTotal = subTotal;
                    vm.ShippingFee = shippingFee;
                    vm.TotalAmount = total;
                    vm.CartItems = cartItems.Select(c => new CartItemSummary
                    {
                        ProductName = c.Product.Name,
                        ProductImage = c.Product.MainImage?.ImagePath,
                        VariantDisplay = c.VariantDisplay,
                        UnitPrice = c.UnitPrice,
                        Quantity = c.Quantity
                    }).ToList();
                    return View(vm);
                }
            }

            // ── Tạo OrderCode unique ──
            var orderCode = GenerateOrderCode();
            while (await _db.Orders.AnyAsync(o => o.OrderCode == orderCode))
                orderCode = GenerateOrderCode();

            // ── Tạo Order ──
            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                FullName = vm.FullName.Trim(),
                Phone = vm.Phone.Trim(),
                Email = vm.Email?.Trim(),
                Province = vm.Province.Trim(),
                District = vm.District.Trim(),
                Ward = vm.Ward.Trim(),
                Address = vm.Address.Trim(),
                Note = vm.Note?.Trim(),
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = OrderStatus.Pending,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                TotalAmount = total,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // cần OrderId

            // ── Tạo OrderItems (snapshot) ──
            foreach (var item in cartItems)
            {
                // Build VariantDisplay trực tiếp từ Attr — không dùng computed property
                // để tránh lưu "" hay "Mặc định" vào DB
                string? variantDisplay = null;
                if (item.Variant != null)
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(item.Variant.Attr1Value))
                        parts.Add(string.IsNullOrWhiteSpace(item.Variant.Attr1Name)
                            ? item.Variant.Attr1Value
                            : $"{item.Variant.Attr1Name}: {item.Variant.Attr1Value}");
                    if (!string.IsNullOrWhiteSpace(item.Variant.Attr2Value))
                        parts.Add(string.IsNullOrWhiteSpace(item.Variant.Attr2Name)
                            ? item.Variant.Attr2Value
                            : $"{item.Variant.Attr2Name}: {item.Variant.Attr2Value}");
                    if (!string.IsNullOrWhiteSpace(item.Variant.Attr3Value))
                        parts.Add(string.IsNullOrWhiteSpace(item.Variant.Attr3Name)
                            ? item.Variant.Attr3Value
                            : $"{item.Variant.Attr3Name}: {item.Variant.Attr3Value}");
                    variantDisplay = parts.Any() ? string.Join(" / ", parts) : null;
                }

                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductName = item.Product.Name,
                    ProductSlug = item.Product.Slug,
                    ProductImage = item.Product.MainImage?.ImagePath,
                    VariantDisplay = variantDisplay,
                    SKU = item.Variant?.SKU ?? item.Product.SKU,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                });

                // Trừ tồn kho
                if (item.Variant != null)
                    item.Variant.StockQty -= item.Quantity;
            }

            // ── Xóa giỏ hàng ──
            _db.CartItems.RemoveRange(cartItems);

            await _db.SaveChangesAsync();

            // ── Gửi email xác nhận (fire-and-forget, không block luồng) ──
            _ = Task.Run(async () =>
            {
                // Reload order với OrderItems để có đủ data cho email
                var orderForEmail = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
                if (orderForEmail != null)
                    await _emailService.SendOrderConfirmationAsync(orderForEmail);
            });

            // ── COD → Pending, Bank → chờ thanh toán ──
            if (vm.PaymentMethod == PaymentMethod.COD)
            {
                TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {orderCode}";
                return RedirectToAction("Success", new { id = order.OrderId });
            }
            else
            {
                // BankTransfer → trang chờ thanh toán
                return RedirectToAction("Payment", new { id = order.OrderId });
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Checkout/Success/{id} — Đặt hàng thành công
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();
            await _emailService.SendOrderConfirmationAsync(order);

            return View(order);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Checkout/Payment/{id} — Trang chờ thanh toán QR
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id
                                       && o.UserId == userId
                                       && o.PaymentMethod == PaymentMethod.BankTransfer
                                       && o.PaymentStatus == PaymentStatus.Unpaid);

            if (order == null) return NotFound();
            return View(order);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Checkout/CheckPayment/{id} — Polling endpoint
        //  FE gọi mỗi 3s để check trạng thái thanh toán
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CheckPayment(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();

            return Json(new
            {
                paid = order.PaymentStatus == PaymentStatus.Paid,
                status = order.Status.ToString(),
                orderCode = order.OrderCode
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════
        private static string GenerateOrderCode()
        {
            // MAISON + 6 số random → VD: MAISON482931
            var rng = new Random();
            return "NGOSTORE" + rng.Next(100000, 999999).ToString();
        }
    }
}