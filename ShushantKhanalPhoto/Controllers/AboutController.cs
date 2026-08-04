using Microsoft.AspNetCore.Mvc;

namespace ShushantKhanalPhoto.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "About";
            ViewData["ActivePage"] = "About";
            return View();
        }
    }
}
