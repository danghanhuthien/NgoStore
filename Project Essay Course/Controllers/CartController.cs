using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Models;

namespace Project_Essay_Course.Controllers
{
    [Authorize] // Bắt buộc login
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Cart — Trang giỏ hàng
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var items  = await GetCartItemsAsync(userId);
            return View(items);
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Cart/AddToCart — Thêm vào giỏ
        //  Gọi từ Detail page hoặc Index page (AJAX hoặc form)
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int? variantId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User)!;

            // Validate product tồn tại và còn active
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc đã ngừng bán.";
                return RedirectBack();
            }

            // Validate variant nếu có
            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = product.Variants
                    .FirstOrDefault(v => v.VariantId == variantId && v.IsActive);

                if (variant == null)
                {
                    TempData["Error"] = "Biến thể sản phẩm không hợp lệ.";
                    return RedirectBack();
                }

                // Kiểm tra tồn kho
                if (variant.StockQty < quantity)
                {
                    TempData["Error"] = $"Chỉ còn {variant.StockQty} sản phẩm trong kho.";
                    return RedirectBack();
                }
            }
            else
            {
                // Sản phẩm không có variant — kiểm tra tồn kho chung
                if (product.TotalStock < quantity)
                {
                    TempData["Error"] = $"Chỉ còn {product.TotalStock} sản phẩm trong kho.";
                    return RedirectBack();
                }
            }

            // Kiểm tra item đã tồn tại trong giỏ chưa
            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId     == userId
                                       && c.ProductId  == productId
                                       && c.VariantId  == variantId);

            if (existing != null)
            {
                // Đã có → cộng thêm số lượng
                var newQty    = existing.Quantity + quantity;
                var stockLimit = variant?.StockQty ?? product.TotalStock;

                existing.Quantity = Math.Min(newQty, stockLimit);
                existing.AddedAt  = DateTime.UtcNow;
            }
            else
            {
                // Chưa có → thêm mới
                _db.CartItems.Add(new CartItem
                {
                    UserId    = userId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity  = quantity,
                    AddedAt   = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng!";

            // AJAX request → trả về JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var count = await _db.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);
                return Json(new { success = true, cartCount = count });
            }

            return RedirectBack();
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Cart/UpdateQty — Cập nhật số lượng
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQty(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User)!;
            var item   = await _db.CartItems
                .Include(c => c.Variant)
                .Include(c => c.Product).ThenInclude(p => p.Variants)
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (item == null) return NotFound();

            if (quantity <= 0)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Giới hạn theo tồn kho
            var stockLimit = item.Variant?.StockQty ?? item.Product.TotalStock;
            item.Quantity  = Math.Min(quantity, stockLimit);

            await _db.SaveChangesAsync();

            // AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var totalCount  = await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
                var totalAmount = await _db.CartItems
                    .Include(c => c.Product)
                    .Include(c => c.Variant)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                return Json(new
                {
                    success    = true,
                    newQty     = item.Quantity,
                    subTotal   = item.SubTotal.ToString("N0"),
                    grandTotal = totalAmount.Sum(x => x.SubTotal).ToString("N0"),
                    cartCount  = totalCount
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Cart/Remove — Xóa 1 item
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = _userManager.GetUserId(User)!;
            var item   = await _db.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            // AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var remaining = await _db.CartItems
                    .Include(c => c.Product)
                    .Include(c => c.Variant)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                return Json(new
                {
                    success    = true,
                    cartCount  = remaining.Sum(x => x.Quantity),
                    grandTotal = remaining.Sum(x => x.SubTotal).ToString("N0"),
                    isEmpty    = !remaining.Any()
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /Cart/Clear — Xóa toàn bộ giỏ
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = _userManager.GetUserId(User)!;
            var items  = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /Cart/MiniCart — Badge số lượng (dùng cho _Layout header)
        //  Gọi bằng AJAX khi cần refresh badge
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> MiniCart()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Json(new { count = 0 });

            var userId = _userManager.GetUserId(User)!;
            var count  = await _db.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { count });
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════
        private async Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            return await _db.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedAt)
                .ToListAsync();
        }

        private IActionResult RedirectBack()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
            return RedirectToAction("Index", "Product");
        }
    }
}
