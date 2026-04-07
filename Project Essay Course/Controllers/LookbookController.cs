using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Data;

namespace Project_Essay_Course.Controllers
{
    public class LookbookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LookbookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Lookbook
        public async Task<IActionResult> Index()
        {
            var lookbooks = await _context.Lookbooks
                .Where(l => l.IsActive)
                .Include(l => l.Items.Where(i => i.IsActive))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(lookbooks);
        }

        // GET: /Lookbook/{slug}
        public async Task<IActionResult> Detail(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return NotFound();

            var lookbook = await _context.Lookbooks
                .Where(l => l.IsActive && l.Slug == slug)
                .Include(l => l.Items.Where(i => i.IsActive))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Include(l => l.Items.Where(i => i.IsActive))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Variants)
                .OrderBy(l => l.Items.Min(i => i.SortOrder))
                .FirstOrDefaultAsync();

            if (lookbook == null)
                return NotFound();

            // Sort items by SortOrder
            lookbook.Items = lookbook.Items
                .OrderBy(i => i.SortOrder)
                .ToList();

            return View(lookbook);
        }
        public IActionResult Lookbook()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

    }
}
