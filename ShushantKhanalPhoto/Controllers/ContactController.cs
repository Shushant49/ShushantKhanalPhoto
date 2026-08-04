using Microsoft.AspNetCore.Mvc;

namespace ShushantKhanalPhoto.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Contact";
            ViewData["ActivePage"] = "Contact";
            return View();
        }
    }

}
