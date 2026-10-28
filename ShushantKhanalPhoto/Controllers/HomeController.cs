using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Models;
using System.Diagnostics;

namespace ShushantKhanalPhoto.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Home page "Featured Work" strip: every photo tagged "featured" in Cloudinary, newest first
        public async Task<IActionResult> Index()
        {
            var featured = await _context.Photos
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.DateTaken)
                .ToListAsync();

            return View(featured);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
