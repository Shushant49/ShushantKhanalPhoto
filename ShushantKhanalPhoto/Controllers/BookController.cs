using Microsoft.AspNetCore.Mvc;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Models;
using ShushantKhanalPhoto.Services;


namespace ShushantKhanalPhoto.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookController> _logger;

        public BookController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<BookController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // Booking form page (GET)
        public IActionResult Index()
        {
            ViewData["Title"] = "Book a Session";
            ViewData["ActivePage"] = "Book";
            return View();
        }

        // Booking form submission (POST)
        [HttpPost]
        public async Task<IActionResult> Submit(BookingRequest booking)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Book a Session";
                ViewData["ActivePage"] = "Book";
                return View("Index", booking);
            }

            try
            {
                // Save to database
                _context.BookingRequests.Add(booking);
                await _context.SaveChangesAsync();

                // Send notification email to YOU
                await _emailService.SendBookingNotificationAsync(booking);

                // Send confirmation email to CUSTOMER
                await _emailService.SendBookingConfirmationAsync(booking);

                TempData["Message"] = "Thank you! Your booking request has been received. Check your email for confirmation.";

                return RedirectToAction("Index", "Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing booking: {ex.Message}");
                TempData["Error"] = "Something went wrong. Please try again or email us directly.";
                return RedirectToAction("Index");
            }
        }
    }
}