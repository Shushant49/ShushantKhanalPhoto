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
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Category>().Ignore(c => c.CloudinaryFolder);

            // Each category gets a matching Cloudinary folder (portfolio/<Name>)
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Portrait" },
                new Category { Id = 2, Name = "Prom" },
                new Category { Id = 3, Name = "Graduation" },
                new Category { Id = 4, Name = "Event" });

            modelBuilder.Entity<Photo>().HasIndex(p => p.CloudinaryPublicId).IsUnique();
        }
    }
}
