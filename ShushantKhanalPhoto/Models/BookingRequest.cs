using System.ComponentModel.DataAnnotations;

namespace ShushantKhanalPhoto.Models
{
    public class BookingRequest
    {

        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "Please enter your name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select a session type")]
        [Display(Name = "Session Type")]
        public string SessionType { get; set; } = string.Empty;

        [Display(Name = "Preferred Date")]
        [DataType(DataType.Date)]
        public DateTime? PreferredDate { get; set; }

        [Display(Name = "Preferred Time")]
        [DataType(DataType.Time)]
        public TimeSpan? PreferredTime { get; set; }

        [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        [Display(Name = "Project Details")]
        public string? Message { get; set; }

        // Optional: Add created timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}