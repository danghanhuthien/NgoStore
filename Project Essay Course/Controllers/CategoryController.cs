using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;
using Project_Essay_Course.Helpers;
using Project_Essay_Course.Models;
using Project_Essay_Course.ViewModels.Category_ViewModel;

namespace Project_Essay_Course.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════════
        //  GET: /Category  — Danh sách
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? search, bool? isActive)
        {
            var query = _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query  = query.Where(c => c.Name.ToLower().Contains(search)
                                       || c.Slug.ToLower().Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            // Lấy root categories trước, sắp xếp theo DisplayOrder
            var all = await query
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Search   = search;
            ViewBag.IsActive = isActive;
            ViewBag.TotalRoot = all.Count(c => c.ParentId == null);
            ViewBag.TotalSub  = all.Count(c => c.ParentId != null);
            ViewBag.TotalActive = all.Count(c => c.IsActive);

            return View(all);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET: /Category/Detail/5
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children.OrderBy(ch => ch.DisplayOrder))
                .Include(c => c.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();
            return View(category);
        }

        // ══════════════════════════════════════════════════════════════
        //  GET: /Category/Create
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateParentDropdownAsync();
            return View(new CategoryCreateViewModel());
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateParentDropdownAsync();
                return View(vm);
            }

            // Kiểm tra slug trùng
            if (await _db.Categories.AnyAsync(c => c.Slug == vm.Slug))
            {
                ModelState.AddModelError(nameof(vm.Slug), "Slug này đã tồn tại.");
                await PopulateParentDropdownAsync();
                return View(vm);
            }

            // Không cho phép tự làm cha của chính mình (dù Create thì không thể nhưng bảo vệ chắc chắn)
            var category = new Category
            {
                Name         = vm.Name.Trim(),
                Slug         = vm.Slug.Trim().ToLower(),
                ParentId     = vm.ParentId,
                DisplayOrder = vm.DisplayOrder,
                IsActive     = vm.IsActive
            };

            // Upload ảnh
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var (success, path, error) = await ImageHelper.UploadAsync(
                    vm.ImageFile, _env.WebRootPath, "categories");

                if (!success)
                    TempData["Warning"] = $"Upload ảnh thất bại: {error}";
                else
                    category.ImagePath = path;
            }

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo danh mục \"{category.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  GET: /Category/Edit/5
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            await PopulateParentDropdownAsync(id, category.ParentId);

            var vm = new CategoryEditViewModel
            {
                CategoryId   = category.CategoryId,
                Name         = category.Name,
                Slug         = category.Slug,
                ParentId     = category.ParentId,
                DisplayOrder = category.DisplayOrder,
                IsActive     = category.IsActive,
                ExistingImagePath = category.ImagePath
            };

            return View(vm);
        }

        // POST: /Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryEditViewModel vm)
        {
            if (id != vm.CategoryId) return BadRequest();

            if (!ModelState.IsValid)
            {
                await PopulateParentDropdownAsync(id, vm.ParentId);
                return View(vm);
            }

            // Không cho phép tự set ParentId = chính nó
            if (vm.ParentId == id)
            {
                ModelState.AddModelError(nameof(vm.ParentId), "Không thể chọn chính danh mục này làm cha.");
                await PopulateParentDropdownAsync(id, vm.ParentId);
                return View(vm);
            }

            // Kiểm tra slug trùng (trừ chính nó)
            if (await _db.Categories.AnyAsync(c => c.Slug == vm.Slug && c.CategoryId != id))
            {
                ModelState.AddModelError(nameof(vm.Slug), "Slug này đã tồn tại.");
                await PopulateParentDropdownAsync(id, vm.ParentId);
                return View(vm);
            }

            // Chặn circular reference: không set parent là con cháu của mình
            if (vm.ParentId.HasValue && await IsDescendantAsync(id, vm.ParentId.Value))
            {
                ModelState.AddModelError(nameof(vm.ParentId), "Không thể chọn danh mục con làm cha.");
                await PopulateParentDropdownAsync(id, vm.ParentId);
                return View(vm);
            }

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name         = vm.Name.Trim();
            category.Slug         = vm.Slug.Trim().ToLower();
            category.ParentId     = vm.ParentId;
            category.DisplayOrder = vm.DisplayOrder;
            category.IsActive     = vm.IsActive;

            // Upload ảnh mới
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                // Xóa ảnh cũ
                ImageHelper.Delete(category.ImagePath, _env.WebRootPath);

                var (success, path, error) = await ImageHelper.UploadAsync(
                    vm.ImageFile, _env.WebRootPath, "categories");

                if (!success)
                    TempData["Warning"] = $"Upload ảnh thất bại: {error}";
                else
                    category.ImagePath = path;
            }

            // Xóa ảnh nếu được yêu cầu
            if (vm.RemoveImage && !string.IsNullOrEmpty(category.ImagePath))
            {
                ImageHelper.Delete(category.ImagePath, _env.WebRootPath);
                category.ImagePath = null;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật danh mục \"{category.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  GET: /Category/Delete/5
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();
            return View(category);
        }

        // POST: /Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();

            // Không xóa nếu còn sub-category
            if (category.Children.Any())
            {
                TempData["Error"] = $"Không thể xóa \"{category.Name}\" vì còn {category.Children.Count} danh mục con. Xóa hoặc chuyển danh mục con trước.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Không xóa nếu còn sản phẩm
            if (category.Products.Any())
            {
                TempData["Error"] = $"Không thể xóa \"{category.Name}\" vì còn {category.Products.Count} sản phẩm. Chuyển sản phẩm sang danh mục khác trước.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Xóa ảnh
            ImageHelper.Delete(category.ImagePath, _env.WebRootPath);

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa danh mục \"{category.Name}\"!";
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  POST: /Category/ToggleActive/5
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = category.IsActive
                ? $"\"{category.Name}\" đã được kích hoạt."
                : $"\"{category.Name}\" đã bị ẩn.";

            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════════════════
        //  POST: /Category/ReOrder  — Cập nhật thứ tự hiển thị
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReOrder([FromBody] List<ReOrderItem> items)
        {
            if (items == null || !items.Any())
                return BadRequest();

            foreach (var item in items)
            {
                var cat = await _db.Categories.FindAsync(item.Id);
                if (cat != null)
                    cat.DisplayOrder = item.Order;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        // ══════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Kiểm tra xem targetId có phải là con/cháu của categoryId không.
        /// Ngăn circular reference khi set ParentId.
        /// </summary>
        private async Task<bool> IsDescendantAsync(int categoryId, int targetId)
        {
            var all = await _db.Categories.ToListAsync();
            var visited = new HashSet<int>();
            return CheckDescendant(categoryId, targetId, all, visited);
        }

        private bool CheckDescendant(int parentId, int targetId, List<Category> all, HashSet<int> visited)
        {
            if (visited.Contains(parentId)) return false;
            visited.Add(parentId);

            foreach (var child in all.Where(c => c.ParentId == parentId))
            {
                if (child.CategoryId == targetId) return true;
                if (CheckDescendant(child.CategoryId, targetId, all, visited)) return true;
            }
            return false;
        }

        /// <summary>
        /// Dropdown danh mục cha — loại trừ chính nó và con cháu của nó.
        /// </summary>
        private async Task PopulateParentDropdownAsync(int? excludeId = null, int? selectedId = null)
        {
            var all = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Lấy tất cả IDs cần loại trừ (chính nó + con cháu)
            var excludedIds = new HashSet<int>();
            if (excludeId.HasValue)
            {
                excludedIds.Add(excludeId.Value);
                CollectDescendantIds(excludeId.Value, all, excludedIds);
            }

            var items = new List<SelectListItem>
            {
                new SelectListItem("— Không có (Root category) —", "")
            };

            foreach (var root in all.Where(c => c.ParentId == null && !excludedIds.Contains(c.CategoryId)))
            {
                items.Add(new SelectListItem(root.Name, root.CategoryId.ToString()));
                foreach (var child in all.Where(c => c.ParentId == root.CategoryId && !excludedIds.Contains(c.CategoryId)))
                    items.Add(new SelectListItem($"— {child.Name}", child.CategoryId.ToString()));
            }

            ViewBag.ParentCategories = new SelectList(items, "Value", "Text", selectedId?.ToString());
        }

        private void CollectDescendantIds(int parentId, List<Category> all, HashSet<int> result)
        {
            foreach (var child in all.Where(c => c.ParentId == parentId))
            {
                result.Add(child.CategoryId);
                CollectDescendantIds(child.CategoryId, all, result);
            }
        }
    }

    // DTO cho ReOrder API
    public class ReOrderItem
    {
        public int Id    { get; set; }
        public int Order { get; set; }
    }
}
