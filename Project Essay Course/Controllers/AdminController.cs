using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;

namespace Project_Essay_Course.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin  — Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ── Thống kê nhanh ──
            var totalProducts   = await _db.Products.CountAsync();
            var activeProducts  = await _db.Products.CountAsync(p => p.IsActive);
            var outOfStock      = await _db.Products.CountAsync(p =>
                                      !p.Variants.Any() || p.Variants.All(v => v.StockQty == 0));
            var totalCategories = await _db.Categories.CountAsync();
            var totalVariants   = await _db.ProductVariants.CountAsync();

            // Sản phẩm mới nhất (5 cái)
            var latestProducts = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Sản phẩm sắp hết hàng (stock 1–5)
            var lowStock = await _db.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .ToListAsync();
            lowStock = lowStock
                .Where(p => p.TotalStock > 0 && p.TotalStock <= 5)
                .OrderBy(p => p.TotalStock)
                .Take(5)
                .ToList();

            ViewBag.TotalProducts   = totalProducts;
            ViewBag.ActiveProducts  = activeProducts;
            ViewBag.OutOfStock      = outOfStock;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalVariants   = totalVariants;
            ViewBag.LatestProducts  = latestProducts;
            ViewBag.LowStockProducts = lowStock;

            return View();
        }
    }
}
