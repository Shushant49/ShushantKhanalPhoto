using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Models;

namespace ShushantKhanalPhoto.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<Photo> Photos { get; set; }  // Optional: store photos in DB too
    }
}
