using ShushantKhanalPhoto.Models;

namespace ShushantKhanalPhoto.Services
{
    public interface IEmailService
    {
        Task SendBookingNotificationAsync(BookingRequest booking);
        Task SendBookingConfirmationAsync(BookingRequest booking);
    }
}