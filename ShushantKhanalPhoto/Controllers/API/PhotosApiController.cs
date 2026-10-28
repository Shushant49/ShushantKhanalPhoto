using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Filters;
using ShushantKhanalPhoto.Models;
using ShushantKhanalPhoto.Services;

namespace PhotographyPortfolio.Controllers.Api
{
    [ApiController]
    [Route("api/photos")]
    public class PhotosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PhotosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/photos?category=Landscape
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos([FromQuery] string? category)
        {
            var query = _context.Photos.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            var photos = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.DateTaken)
                .ToListAsync();

            return Ok(photos);
        }

        // GET api/photos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Photo>> GetPhoto(int id)
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
                return NotFound(new { message = $"Photo with id {id} not found." });

            return Ok(photo);
        }

        // GET api/photos/featured
        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<Photo>>> GetFeatured()
        {
            var photos = await _context.Photos
                .Where(p => p.IsFeatured)
                .ToListAsync();

            return Ok(photos);
        }

        // GET api/photos/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        // POST api/photos/upload — upload the image file into its category's Cloudinary folder and save its details (multipart/form-data)
        [HttpPost("upload")]
        [RequireApiKey]
        [RequestSizeLimit(20_000_000)]  // 20 MB
        public async Task<ActionResult<Photo>> UploadPhoto(
            IFormFile file,
            [FromForm] string title,
            [FromForm] string? description,
            [FromForm] string category,
            [FromForm] DateTime? dateTaken,
            [FromForm] bool isFeatured,
            [FromServices] Cloudinary cloudinary)
        {
            if (file.Length == 0 || !file.ContentType.StartsWith("image/"))
                return BadRequest(new { message = "Please upload a non-empty image file." });

            var matchedCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == category);
            if (matchedCategory == null)
            {
                var valid = await _context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
                return BadRequest(new { message = $"Unknown category '{category}'. Use one of: {string.Join(", ", valid)}." });
            }

            // Title/description/featured are stored on the Cloudinary asset too, so the folder sync keeps them
            var context = new StringDictionary();
            context.Add("caption", title);
            if (!string.IsNullOrWhiteSpace(description))
                context.Add("alt", description);

            await using var stream = file.OpenReadStream();
            var result = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                AssetFolder = matchedCategory.CloudinaryFolder,
                Context = context,
                Tags = isFeatured ? CloudinarySyncService.FeaturedTag : null
            });

            if (result.Error != null)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = result.Error.Message });

            var photo = new Photo
            {
                Title = title,
                Description = description,
                Category = matchedCategory.Name,
                DateTaken = dateTaken ?? DateTime.UtcNow,
                IsFeatured = isFeatured,
                ImageUrl = result.SecureUrl.ToString(),
                CloudinaryPublicId = result.PublicId
            };

            // A background sync may have already picked the new asset up from its folder
            var synced = await _context.Photos.FirstOrDefaultAsync(p => p.CloudinaryPublicId == result.PublicId);
            if (synced != null)
                return CreatedAtAction(nameof(GetPhoto), new { id = synced.Id }, synced);

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, photo);
        }

        // POST api/photos/sync — pull changes from the Cloudinary category folders right now
        [HttpPost("sync")]
        [RequireApiKey]
        public async Task<ActionResult<SyncResult>> Sync([FromServices] CloudinarySyncService sync)
        {
            await sync.EnsureFoldersAsync();
            return Ok(await sync.SyncAsync(HttpContext.RequestAborted));
        }

        // POST api/photos — add a photo that's already hosted somewhere (JSON body with ImageUrl)
        [HttpPost]
        [RequireApiKey]
        public async Task<ActionResult<Photo>> CreatePhoto(Photo photo)
        {
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, photo);
        }

        // DELETE api/photos/5 — also deletes the image from Cloudinary, otherwise the folder sync would bring it back
        [HttpDelete("{id}")]
        [RequireApiKey]
        public async Task<IActionResult> DeletePhoto(int id, [FromServices] Cloudinary cloudinary)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
                return NotFound();

            if (photo.CloudinaryPublicId != null)
            {
                var result = await cloudinary.DestroyAsync(new DeletionParams(photo.CloudinaryPublicId) { Invalidate = true });
                if (result.Error != null || (result.Result != "ok" && result.Result != "not found"))
                    return StatusCode(StatusCodes.Status502BadGateway, new { message = result.Error?.Message ?? $"Cloudinary returned '{result.Result}'." });
            }

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}