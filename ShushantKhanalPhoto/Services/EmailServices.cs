using SendGrid;
using SendGrid.Helpers.Mail;
using ShushantKhanalPhoto.Models;

namespace ShushantKhanalPhoto.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Email to YOU when someone books
        public async Task SendBookingNotificationAsync(BookingRequest booking)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(
                _configuration["SendGrid:FromEmail"],
                _configuration["SendGrid:FromName"]
            );

            var to = new EmailAddress(_configuration["SendGrid:NotificationEmail"]);

            var subject = $"New Booking Request - {booking.SessionType}";

            string preferredTimeFormatted = booking.PreferredTime.HasValue
                ? DateTime.Today.Add(booking.PreferredTime.Value).ToString("h:mm tt")
                : "Not specified";


            var htmlContent = $@"
                <h2>New Booking Request Received</h2>
                <p><strong>Name:</strong> {booking.Name}</p>
                <p><strong>Email:</strong> {booking.Email}</p>
                <p><strong>Phone:</strong> {booking.Phone ?? "Not provided"}</p>
                <p><strong>Session Type:</strong> {booking.SessionType}</p>
                <p><strong>Preferred Date:</strong> {booking.PreferredDate?.ToString("MMMM dd, yyyy") ?? "Not specified"}</p>
                <p><strong>Preferred Time:</strong> {preferredTimeFormatted}</p>
                <p><strong>Message:</strong><br/>{booking.Message ?? "No message provided"}</p>
                <p><strong>Submitted:</strong> {booking.CreatedAt.ToString("MMMM dd, yyyy hh:mm tt")}</p>
            ";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                _logger.LogInformation($"Notification email sent. Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send notification email: {ex.Message}");
                throw;
            }
        }

        // Confirmation email to CUSTOMER
        public async Task SendBookingConfirmationAsync(BookingRequest booking)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(
                _configuration["SendGrid:FromEmail"],
                _configuration["SendGrid:FromName"]
            );

            var to = new EmailAddress(booking.Email, booking.Name);

            var subject = "Thank You for Your Booking Request";

            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #c8a96e;'>Thank You, {booking.Name}!</h2>
                    
                    <p>I've received your booking request for a <strong>{booking.SessionType}</strong> session.</p>
                    
                    <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0;'>Your Request Details:</h3>
                        <p><strong>Session Type:</strong> {booking.SessionType}</p>
                        <p><strong>Preferred Date:</strong> {booking.PreferredDate?.ToString("MMMM dd, yyyy") ?? "Not specified"}</p>
                        <p><strong>Preferred Time:</strong> {booking.PreferredTime?.ToString(@"hh\:mm tt") ?? "Not specified"}</p>
                    </div>
                    
                    <p>I'll review your request and get back to you within 24 hours with my availability, package options, and pricing details.</p>
                    
                    <p>If you have any urgent questions in the meantime, feel free to email me directly at <a href='mailto:khanalshushant49@gmail.com'>khanalshushant49@gmail.com</a> or call +1 (308) 240-1902.</p>
                    
                    <p style='margin-top: 30px;'>Looking forward to working with you!</p>
                    
                    <p style='color: #666;'>
                        Best regards,<br/>
                        <strong>Shu_Kha Photography</strong>
                    </p>
                </div>
            ";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                _logger.LogInformation($"Confirmation email sent to {booking.Email}. Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send confirmation email: {ex.Message}");
                // Don't throw - we don't want to fail the booking if confirmation email fails
            }
        }
    }
}