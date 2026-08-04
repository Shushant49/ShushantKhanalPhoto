using Microsoft.AspNetCore.Mvc;
using ShushantKhanalPhoto.Models;

namespace ShushantKhanalPhoto.Controllers
{
    public class GalleryController : Controller
    {
        // Sample photo data (replace with database later)
        private static readonly List<Photo> _photos = new()
        {
            new Photo { Id = 1, Title = "Golden Hour", Description = "Sunset over rolling hills", Category = "Prom", IsFeatured = true, DateTaken = new DateTime(2024, 6, 10), ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800" },
            new Photo { Id = 2, Title = "City Lights", Description = "Urban nightscape", Category = "Graduation", IsFeatured = false, DateTaken = new DateTime(2024, 7, 22), ImageUrl = "https://images.unsplash.com/photo-1514565131-fce0801e5785?w=800" },
            new Photo { Id = 3, Title = "Morning Mist", Description = "Foggy mountain valleys", Category = "Prom", IsFeatured = true, DateTaken = new DateTime(2024, 8, 5), ImageUrl = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=800" },
            new Photo { Id = 4, Title = "Portrait I", Description = "Natural light portrait", Category = "Portrait", IsFeatured = false, DateTaken = new DateTime(2024, 9, 1), ImageUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=800" },
            new Photo { Id = 5, Title = "Rainy Graduation", Description = "Reflections in the city", Category = "Graduation", IsFeatured = false, DateTaken = new DateTime(2024, 9, 14), ImageUrl = "https://images.unsplash.com/photo-1449824913935-59a10b8d2000?w=800" },
            new Photo { Id = 6, Title = "Autumn Path", Description = "Fall colors and trails", Category = "Prom", IsFeatured = false, DateTaken = new DateTime(2024, 10, 3), ImageUrl = "https://images.unsplash.com/photo-1511497584788-876760111969?w=800" },
            new Photo { Id = 7, Title = "Urban Lines", Description = "Architectural details", Category = "Graduation", IsFeatured = false, DateTaken = new DateTime(2024, 10, 15), ImageUrl = "https://images.unsplash.com/photo-1480714378408-67cf0d13bc1b?w=800" },
            new Photo { Id = 8, Title = "Portrait II", Description = "Studio session", Category = "Portrait", IsFeatured = false, DateTaken = new DateTime(2024, 11, 2), ImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800" },
            new Photo { Id = 9, Title = "Coastal Sunset", Description = "Ocean waves at dusk", Category = "Prom", IsFeatured = true, DateTaken = new DateTime(2024, 11, 15), ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800" },
            new Photo { Id = 10, Title = "Night Portrait", Description = "Urban portrait with neon", Category = "Portrait", IsFeatured = false, DateTaken = new DateTime(2024, 12, 1), ImageUrl = "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=800" },
            new Photo { Id = 11, Title = "City Reflections", Description = "Glass and steel architecture", Category = "Graduation", IsFeatured = false, DateTaken = new DateTime(2024, 12, 10), ImageUrl = "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?w=800" },
            new Photo { Id = 12, Title = "Mountain Peak", Description = "Alpine Prom at sunrise", Category = "Prom", IsFeatured = false, DateTaken = new DateTime(2025, 1, 5), ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800" },
        };

        // GET /Gallery — show all photos, optional category filter
        public IActionResult Index(string? category)
        {
            ViewData["Title"] = "Gallery";
            ViewData["ActivePage"] = "Gallery";

            var photos = _photos.AsEnumerable();

            if (!string.IsNullOrEmpty(category))
                photos = photos.Where(p => p.Category == category);

            // Featured photos first, then newest
            photos = photos
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.DateTaken);

            ViewBag.Categories = _photos.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
            ViewBag.ActiveFilter = category;

            return View(photos.ToList());
        }

        // GET /Gallery/Detail/5 — single photo page
        public IActionResult Detail(int id)
        {
            ViewData["Title"] = "Photo Detail";
            ViewData["ActivePage"] = "Gallery";

            var photo = _photos.FirstOrDefault(p => p.Id == id);

            if (photo == null)
                return NotFound();

            // Suggest other photos from the same category
            ViewBag.Related = _photos
                .Where(p => p.Category == photo.Category && p.Id != id)
                .Take(3)
                .ToList();

            return View(photo);
        }
    }
}