using System.ComponentModel.DataAnnotations;

namespace ShushantKhanalPhoto.Models
{
    public class Category
    {
        public const string RootFolder = "portfolio";

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;  // e.g. "Portrait", "Prom"

        // Cloudinary folder whose images belong to this category, e.g. "portfolio/Prom"
        public string CloudinaryFolder => $"{RootFolder}/{Name}";
    }
}
