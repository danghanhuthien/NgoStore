using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Helpers;
using Project_Essay_Course.Models;
using Project_Essay_Course.ViewModels.Product_ViewModel;

namespace Project_Essay_Course.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════════
        //  PUBLIC — Người dùng xem
        // ══════════════════════════════════════════════════════════════

        // GET: /Product
        // Trang danh sách sản phẩm — filter theo category, giá, search
        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? search,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort,
            int page = 1)
        {
            const int pageSize = 12;

            var query = _db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            // Filter theo category (bao gồm sub-category)
            if (categoryId.HasValue)
            {
                var categoryIds = await GetCategoryWithChildrenIdsAsync(categoryId.Value);
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }

            // Filter theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.ShortDesc != null && p.ShortDesc.ToLower().Contains(search)));
            }

            // Filter theo giá
            if (minPrice.HasValue)
                query = query.Where(p =>
                    (p.SalePrice.HasValue ? p.SalePrice : p.BasePrice) >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(p =>
                    (p.SalePrice.HasValue ? p.SalePrice : p.BasePrice) <= maxPrice);

            // Sort
            query = sort switch
            {
                "price_asc"  => query.OrderBy(p => p.SalePrice.HasValue ? p.SalePrice : p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.SalePrice.HasValue ? p.SalePrice : p.BasePrice),
                "newest"     => query.OrderByDescending(p => p.CreatedAt),
                "name_asc"   => query.OrderBy(p => p.Name),
                _            => query.OrderByDescending(p => p.IsFeatured)
                                     .ThenByDescending(p => p.CreatedAt)
            };

            // Pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var products   = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Categories cho sidebar filter
            var categories = await _db.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .Include(c => c.Children.Where(ch => ch.IsActive))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var vm = new ProductListViewModel
            {
                Products      = products,
                Categories    = categories,
                CurrentCategoryId = categoryId,
                Search        = search,
                MinPrice      = minPrice,
                MaxPrice      = maxPrice,
                Sort          = sort,
                CurrentPage   = page,
                TotalPages    = totalPages,
                TotalItems    = totalItems
            };

            return View(vm);
        }

        // GET: /Product/Detail/5  hoặc  /Product/Detail/ao-linen-dang-rong
        [HttpGet]
        public async Task<IActionResult> Detail(int? id, string? slug)
        {
            Product? product = null;

            if (id.HasValue)
            {
                product = await _db.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .FirstOrDefaultAsync(p => p.ProductId == id);
            }
            else if (!string.IsNullOrEmpty(slug))
            {
                product = await _db.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                    .Include(p => p.Variants.Where(v => v.IsActive))
                    .FirstOrDefaultAsync(p => p.Slug == slug);
            }

            if (product == null) return NotFound();

            // Sản phẩm liên quan (cùng category, tối đa 4)
            var related = await _db.Products
                .Where(p => p.IsActive
                    && p.CategoryId == product.CategoryId
                    && p.ProductId  != product.ProductId)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.IsFeatured)
                .Take(4)
                .ToListAsync();

            var vm = new ProductDetailViewModel
            {
                Product  = product,
                Related  = related
            };

            return View(vm);
        }

        // GET: /Product/Women  — shortcut filter nữ
        [HttpGet]
        public Task<IActionResult> Women(string? sort, int page = 1)
            => RedirectToCategory("nu", sort, page);

        // GET: /Product/Men
        [HttpGet]
        public Task<IActionResult> Men(string? sort, int page = 1)
            => RedirectToCategory("nam", sort, page);

        // GET: /Product/Kids
        [HttpGet]
        public Task<IActionResult> Kids(string? sort, int page = 1)
            => RedirectToCategory("tre-em", sort, page);

        // GET: /Product/Sale
        [HttpGet]
        public async Task<IActionResult> Sale(string? sort, int page = 1)
        {
            const int pageSize = 12;

            var query = _db.Products
                .Where(p => p.IsActive && p.SalePrice.HasValue && p.SalePrice < p.BasePrice)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            query = sort switch
            {
                "price_asc"  => query.OrderBy(p => p.SalePrice),
                "price_desc" => query.OrderByDescending(p => p.SalePrice),
                _            => query.OrderByDescending(p => p.UpdatedAt)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var products   = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ProductListViewModel
            {
                Products    = products,
                Categories  = new List<Category>(),
                Sort        = sort,
                CurrentPage = page,
                TotalPages  = totalPages,
                TotalItems  = totalItems
            };

            ViewBag.PageTitle = "Sale — Giảm Giá Đặc Biệt";
            return View("Index", vm);
        }

        // ══════════════════════════════════════════════════════════════
        //  ADMIN — Quản lý sản phẩm  [Authorize(Roles = "Admin")]
        // ══════════════════════════════════════════════════════════════

        // GET: /Product/Manage
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage(
            string? search,
            int? categoryId,
            bool? isActive,
            int page = 1)
        {
            const int pageSize = 15;

            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query  = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(search)));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var products   = await query
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories  = new SelectList(
                await _db.Categories.Where(c => c.IsActive).ToListAsync(),
                "CategoryId", "Name", categoryId);
            ViewBag.Search      = search;
            ViewBag.CategoryId  = categoryId;
            ViewBag.IsActive    = isActive;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages  = totalPages;
            ViewBag.TotalItems  = totalItems;

            return View(products);
        }

        // GET: /Product/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoryDropdownAsync();
            return View(new ProductCreateViewModel());
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoryDropdownAsync();
                return View(vm);
            }

            // Kiểm tra slug trùng
            if (await _db.Products.AnyAsync(p => p.Slug == vm.Slug))
            {
                ModelState.AddModelError(nameof(vm.Slug), "Slug này đã tồn tại, vui lòng chọn slug khác.");
                await PopulateCategoryDropdownAsync();
                return View(vm);
            }

            // Tạo Product
            var product = new Product
            {
                Name        = vm.Name.Trim(),
                Slug        = vm.Slug.Trim().ToLower(),
                Description = vm.Description,
                ShortDesc   = vm.ShortDesc,
                BasePrice   = vm.BasePrice,
                SalePrice   = vm.SalePrice,
                SKU         = vm.SKU,
                IsFeatured  = vm.IsFeatured,
                IsActive    = vm.IsActive,
                CategoryId  = vm.CategoryId,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(); // cần ProductId trước khi save ảnh/variant

            // Upload ảnh
            if (vm.Images != null && vm.Images.Any())
            {
                var isFirst = true;
                foreach (var file in vm.Images.Where(f => f.Length > 0))
                {
                    var (success, path, error) = await ImageHelper.UploadAsync(
                        file, _env.WebRootPath, "products");

                    if (!success)
                    {
                        TempData["Warning"] = $"Có ảnh upload thất bại: {error}";
                        continue;
                    }

                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId    = product.ProductId,
                        ImagePath    = path!,
                        AltText      = product.Name,
                        IsMain       = isFirst, // ảnh đầu tiên = ảnh đại diện
                        DisplayOrder = product.Images.Count
                    });
                    isFirst = false;
                }
            }

            // Tạo Variants
            if (vm.Variants != null && vm.Variants.Any())
            {
                foreach (var v in vm.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Attr1Value) || v.StockQty > 0))
                {
                    _db.ProductVariants.Add(new ProductVariant
                    {
                        ProductId     = product.ProductId,
                        Attr1Name     = v.Attr1Name,
                        Attr1Value    = v.Attr1Value,
                        Attr2Name     = v.Attr2Name,
                        Attr2Value    = v.Attr2Value,
                        Attr3Name     = v.Attr3Name,
                        Attr3Value    = v.Attr3Value,
                        StockQty      = v.StockQty,
                        PriceOverride = v.PriceOverride,
                        SKU           = v.SKU,
                        IsActive      = true
                    });
                }
            }
            else
            {
                // Không có variant → tạo 1 variant mặc định
                _db.ProductVariants.Add(new ProductVariant
                {
                    ProductId = product.ProductId,
                    StockQty  = vm.DefaultStock,
                    IsActive  = true
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo sản phẩm \"{product.Name}\" thành công!";
            return RedirectToAction(nameof(Manage));
        }

        // GET: /Product/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            await PopulateCategoryDropdownAsync(product.CategoryId);

            var vm = new ProductEditViewModel
            {
                ProductId   = product.ProductId,
                Name        = product.Name,
                Slug        = product.Slug,
                Description = product.Description,
                ShortDesc   = product.ShortDesc,
                BasePrice   = product.BasePrice,
                SalePrice   = product.SalePrice,
                SKU         = product.SKU,
                IsFeatured  = product.IsFeatured,
                IsActive    = product.IsActive,
                CategoryId  = product.CategoryId,
                ExistingImages = product.Images.ToList(),
                Variants    = product.Variants.Select(v => new VariantInputModel
                {
                    VariantId     = v.VariantId,
                    Attr1Name     = v.Attr1Name,
                    Attr1Value    = v.Attr1Value,
                    Attr2Name     = v.Attr2Name,
                    Attr2Value    = v.Attr2Value,
                    Attr3Name     = v.Attr3Name,
                    Attr3Value    = v.Attr3Value,
                    StockQty      = v.StockQty,
                    PriceOverride = v.PriceOverride,
                    SKU           = v.SKU
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel vm)
        {
            if (id != vm.ProductId) return BadRequest();

            if (!ModelState.IsValid)
            {
                await PopulateCategoryDropdownAsync(vm.CategoryId);
                vm.ExistingImages = await _db.ProductImages
                    .Where(i => i.ProductId == id)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();
                return View(vm);
            }

            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            // Kiểm tra slug trùng (trừ chính nó)
            if (await _db.Products.AnyAsync(p => p.Slug == vm.Slug && p.ProductId != id))
            {
                ModelState.AddModelError(nameof(vm.Slug), "Slug này đã tồn tại.");
                await PopulateCategoryDropdownAsync(vm.CategoryId);
                vm.ExistingImages = product.Images.ToList();
                return View(vm);
            }

            // Cập nhật thông tin cơ bản
            product.Name        = vm.Name.Trim();
            product.Slug        = vm.Slug.Trim().ToLower();
            product.Description = vm.Description;
            product.ShortDesc   = vm.ShortDesc;
            product.BasePrice   = vm.BasePrice;
            product.SalePrice   = vm.SalePrice;
            product.SKU         = vm.SKU;
            product.IsFeatured  = vm.IsFeatured;
            product.IsActive    = vm.IsActive;
            product.CategoryId  = vm.CategoryId;
            product.UpdatedAt   = DateTime.UtcNow;

            // Xóa ảnh được chọn xóa
            if (vm.DeleteImageIds != null && vm.DeleteImageIds.Any())
            {
                var toDelete = product.Images
                    .Where(i => vm.DeleteImageIds.Contains(i.ImageId))
                    .ToList();

                ImageHelper.DeleteMany(toDelete.Select(i => i.ImagePath), _env.WebRootPath);
                _db.ProductImages.RemoveRange(toDelete);
            }

            // Upload ảnh mới
            if (vm.NewImages != null && vm.NewImages.Any())
            {
                var hasMain = product.Images.Any(i => i.IsMain);
                foreach (var file in vm.NewImages.Where(f => f.Length > 0))
                {
                    var (success, path, error) = await ImageHelper.UploadAsync(
                        file, _env.WebRootPath, "products");

                    if (!success)
                    {
                        TempData["Warning"] = $"Có ảnh upload thất bại: {error}";
                        continue;
                    }

                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId    = product.ProductId,
                        ImagePath    = path!,
                        AltText      = product.Name,
                        IsMain       = !hasMain, // nếu chưa có ảnh main thì set ảnh đầu tiên
                        DisplayOrder = product.Images.Count
                    });
                    hasMain = true;
                }
            }

            // Cập nhật / Thêm / Xóa variants
            // Xóa các variant không còn trong danh sách
            var submittedVariantIds = vm.Variants?
                .Where(v => v.VariantId > 0)
                .Select(v => v.VariantId)
                .ToList() ?? new List<int>();

            var variantsToRemove = product.Variants
                .Where(v => !submittedVariantIds.Contains(v.VariantId))
                .ToList();
            _db.ProductVariants.RemoveRange(variantsToRemove);

            if (vm.Variants != null)
            {
                foreach (var v in vm.Variants)
                {
                    if (v.VariantId > 0)
                    {
                        // Cập nhật variant cũ
                        var existing = product.Variants.FirstOrDefault(x => x.VariantId == v.VariantId);
                        if (existing != null)
                        {
                            existing.Attr1Name     = v.Attr1Name;
                            existing.Attr1Value    = v.Attr1Value;
                            existing.Attr2Name     = v.Attr2Name;
                            existing.Attr2Value    = v.Attr2Value;
                            existing.Attr3Name     = v.Attr3Name;
                            existing.Attr3Value    = v.Attr3Value;
                            existing.StockQty      = v.StockQty;
                            existing.PriceOverride = v.PriceOverride;
                            existing.SKU           = v.SKU;
                        }
                    }
                    else
                    {
                        // Thêm variant mới
                        _db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId     = product.ProductId,
                            Attr1Name     = v.Attr1Name,
                            Attr1Value    = v.Attr1Value,
                            Attr2Name     = v.Attr2Name,
                            Attr2Value    = v.Attr2Value,
                            Attr3Name     = v.Attr3Name,
                            Attr3Value    = v.Attr3Value,
                            StockQty      = v.StockQty,
                            PriceOverride = v.PriceOverride,
                            SKU           = v.SKU,
                            IsActive      = true
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật sản phẩm \"{product.Name}\" thành công!";
            return RedirectToAction(nameof(Manage));
        }

        // GET: /Product/AdminDetail/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDetail(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants.OrderBy(v => v.VariantId))
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Product/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            // Xóa file ảnh khỏi wwwroot
            ImageHelper.DeleteMany(
                product.Images.Select(i => i.ImagePath),
                _env.WebRootPath);

            _db.Products.Remove(product); // cascade delete variants + images
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa sản phẩm \"{product.Name}\"!";
            return RedirectToAction(nameof(Manage));
        }

        // POST: /Product/SetMainImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetMainImage(int imageId, int productId)
        {
            var images = await _db.ProductImages
                .Where(i => i.ProductId == productId)
                .ToListAsync();

            foreach (var img in images)
                img.IsMain = img.ImageId == imageId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // POST: /Product/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive  = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = product.IsActive
                ? $"\"{product.Name}\" đã được kích hoạt."
                : $"\"{product.Name}\" đã bị ẩn.";

            return RedirectToAction(nameof(Manage));
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy ID của category + tất cả sub-category con cháu.
        /// Dùng để filter sản phẩm khi click vào category cha.
        /// </summary>
        private async Task<List<int>> GetCategoryWithChildrenIdsAsync(int categoryId)
        {
            var all = await _db.Categories.ToListAsync();
            var result = new List<int>();
            CollectIds(categoryId, all, result);
            return result;
        }

        private void CollectIds(int parentId, List<Category> all, List<int> result)
        {
            result.Add(parentId);
            foreach (var child in all.Where(c => c.ParentId == parentId))
                CollectIds(child.CategoryId, all, result);
        }

        /// <summary>
        /// Populate dropdown category cho Create/Edit form.
        /// Hiển thị dạng: "Nữ", "— Áo", "— Váy", "Nam", "— Áo",...
        /// </summary>
        private async Task PopulateCategoryDropdownAsync(int? selectedId = null)
        {
            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var items = new List<SelectListItem>();
            // Root categories
            foreach (var root in categories.Where(c => c.ParentId == null))
            {
                items.Add(new SelectListItem(root.Name, root.CategoryId.ToString()));
                // Sub-categories
                foreach (var child in categories.Where(c => c.ParentId == root.CategoryId))
                    items.Add(new SelectListItem($"— {child.Name}", child.CategoryId.ToString()));
            }

            ViewBag.Categories = new SelectList(items, "Value", "Text", selectedId?.ToString());
        }

        /// <summary>
        /// Helper redirect cho Women/Men/Kids shortcuts.
        /// </summary>
        private async Task<IActionResult> RedirectToCategory(string slug, string? sort, int page)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            return RedirectToAction(nameof(Index), new
            {
                categoryId = category?.CategoryId,
                sort,
                page
            });
        }
    }
}
