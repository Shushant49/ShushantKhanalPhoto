using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Models;
using ShushantKhanalPhoto.Services;

namespace PhotographyPortfolio.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingsApiController> _logger;

        public BookingsApiController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<BookingsApiController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // POST api/bookings
        [HttpPost]
        public async Task<ActionResult<BookingRequest>> CreateBooking(BookingRequest booking)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.BookingRequests.Add(booking);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendBookingNotificationAsync(booking);
                await _emailService.SendBookingConfirmationAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email sending failed: {ex.Message}");
                // Booking is still saved even if the email fails
            }

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }

        // GET api/bookings — admin use
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingRequest>>> GetBookings()
        {
            var bookings = await _context.BookingRequests
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(bookings);
        }

        // GET api/bookings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingRequest>> GetBooking(int id)
        {
            var booking = await _context.BookingRequests.FindAsync(id);

            if (booking == null)
                return NotFound();

            return Ok(booking);
        }

        // DELETE api/bookings/5 — admin use
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.BookingRequests.FindAsync(id);
            if (booking == null)
                return NotFound();

            _context.BookingRequests.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}