using System.ComponentModel.DataAnnotations;

namespace ShushantKhanalPhoto.Models
{
    public class Photo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;  // Cloudinary

        [StringLength(50)]
        public string? Category { get; set; }  // e.g. "Landscape", "Portrait", "Street"

        public DateTime DateTaken { get; set; } = DateTime.UtcNow;

        public bool IsFeatured { get; set; } = false;  // pin favorites to the top

        [StringLength(255)]
        public string? CloudinaryPublicId { get; set; }  // links the row to its Cloudinary asset for syncing
    }
}