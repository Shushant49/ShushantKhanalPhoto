using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;

namespace ShushantKhanalPhoto.Controllers
{
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GalleryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Gallery — show all photos, optional category filter
        public async Task<IActionResult> Index(string? category)
        {
            ViewData["Title"] = "Gallery";
            ViewData["ActivePage"] = "Gallery";

            var query = _context.Photos.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            // Featured photos first, then newest
            var photos = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.DateTaken)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();
            ViewBag.ActiveFilter = category;

            return View(photos);
        }

        // GET /Gallery/Detail/5 — single photo page
        public async Task<IActionResult> Detail(int id)
        {
            ViewData["Title"] = "Photo Detail";
            ViewData["ActivePage"] = "Gallery";

            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
                return NotFound();

            // Suggest other photos from the same category
            ViewBag.Related = await _context.Photos
                .Where(p => p.Category == photo.Category && p.Id != id)
                .Take(3)
                .ToListAsync();

            return View(photo);
        }
    }
}
