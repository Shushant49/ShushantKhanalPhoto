using System.Globalization;
using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using ShushantKhanalPhoto.Data;
using ShushantKhanalPhoto.Models;

namespace ShushantKhanalPhoto.Services
{
    public record SyncResult(int Added, int Updated, int Removed);

    // Keeps the Photos table in step with the category folders in Cloudinary:
    //   portfolio/<Category>/  ->  Photo.Category = <Category>
    // Cloudinary is the source of truth for synced photos:
    //   Media Library "Title" (caption)       -> Photo.Title
    //   Media Library "Description" (alt)     -> Photo.Description
    //   "featured" tag                        -> Photo.IsFeatured
    public class CloudinarySyncService
    {
        public const string FeaturedTag = "featured";

        // Background timer and the manual sync endpoint must never run at the same time
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinarySyncService> _logger;

        public CloudinarySyncService(ApplicationDbContext context, Cloudinary cloudinary, ILogger<CloudinarySyncService> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        // Creates portfolio/<Category> in Cloudinary for every category (no-op if it already exists)
        public async Task EnsureFoldersAsync()
        {
            foreach (var category in await _context.Categories.ToListAsync())
            {
                var result = await _cloudinary.CreateFolderAsync(category.CloudinaryFolder);
                if (result.Error != null)
                    _logger.LogWarning("Could not create Cloudinary folder {Folder}: {Error}", category.CloudinaryFolder, result.Error.Message);
            }
        }

        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                return await SyncInternalAsync(ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<SyncResult> SyncInternalAsync(CancellationToken ct)
        {
            var folderToCategory = (await _context.Categories.ToListAsync(ct))
                .ToDictionary(c => c.CloudinaryFolder, c => c.Name, StringComparer.OrdinalIgnoreCase);

            var assets = await SearchCategoryFoldersAsync(folderToCategory.Keys, ct);
            var photos = await _context.Photos.ToListAsync(ct);
            int added = 0, updated = 0, removed = 0;

            foreach (var asset in assets)
            {
                var photo = photos.FirstOrDefault(p => p.CloudinaryPublicId == asset.PublicId);
                var adopted = false;

                // Adopt rows added before syncing existed (matched by their Cloudinary URL)
                if (photo == null)
                {
                    photo = photos.FirstOrDefault(p => p.CloudinaryPublicId == null && p.ImageUrl.Contains($"/{asset.PublicId}."));
                    if (photo != null)
                    {
                        photo.CloudinaryPublicId = asset.PublicId;
                        adopted = true;
                    }
                }

                if (photo == null)
                {
                    photo = new Photo
                    {
                        CloudinaryPublicId = asset.PublicId,
                        Title = Truncate(TitleFromFileName(asset.DisplayName ?? asset.PublicId), 100),
                        DateTaken = DateTakenFrom(asset)
                    };
                    _context.Photos.Add(photo);
                    photos.Add(photo);
                    added++;
                }

                if ((ApplyAsset(photo, asset, folderToCategory[asset.AssetFolder]) || adopted) && photo.Id != 0)
                    updated++;
            }

            // Rows whose asset no longer shows up in a category folder: deleted or moved out in Cloudinary.
            // Search results can lag a few seconds behind uploads, so confirm with a direct lookup before removing.
            var foundIds = assets.Select(a => a.PublicId).ToHashSet();
            foreach (var photo in photos.Where(p => p.CloudinaryPublicId != null && !foundIds.Contains(p.CloudinaryPublicId)).ToList())
            {
                var resource = await _cloudinary.GetResourceAsync(new GetResourceParams(photo.CloudinaryPublicId) { Context = true, Tags = true });

                if (resource.StatusCode == HttpStatusCode.NotFound
                    || (resource.Error == null && !folderToCategory.ContainsKey(resource.AssetFolder ?? "")))
                {
                    _context.Photos.Remove(photo);
                    removed++;
                }
                else if (resource.Error == null && ApplyResource(photo, resource, folderToCategory[resource.AssetFolder!]))
                {
                    updated++;
                }
            }

            await _context.SaveChangesAsync(ct);

            if (added + updated + removed > 0)
                _logger.LogInformation("Cloudinary sync: {Added} added, {Updated} updated, {Removed} removed", added, updated, removed);

            return new SyncResult(added, updated, removed);
        }

        private async Task<List<SearchResource>> SearchCategoryFoldersAsync(IEnumerable<string> folders, CancellationToken ct)
        {
            var folderFilter = string.Join(" OR ", folders.Select(f => $"asset_folder=\"{f}\""));
            var assets = new List<SearchResource>();
            string? cursor = null;

            do
            {
                var search = _cloudinary.Search()
                    .Expression($"resource_type:image AND ({folderFilter})")
                    .WithField("context")
                    .WithField("tags")
                    .WithField("image_metadata")
                    .MaxResults(500);
                if (cursor != null)
                    search = search.NextCursor(cursor);

                var result = await search.ExecuteAsync(ct);
                if (result.Error != null)
                    throw new InvalidOperationException($"Cloudinary search failed: {result.Error.Message}");

                assets.AddRange(result.Resources);
                cursor = result.NextCursor;
            } while (!string.IsNullOrEmpty(cursor));

            return assets;
        }

        // Copies Cloudinary-managed fields onto the photo; returns true if anything changed
        private static bool ApplyAsset(Photo photo, SearchResource asset, string category) =>
            Apply(photo, category, asset.SecureUrl, asset.Context, asset.Tags);

        private static bool ApplyResource(Photo photo, GetResourceResult resource, string category) =>
            Apply(photo, category, resource.SecureUrl,
                resource.Context?["custom"]?.ToObject<Dictionary<string, string>>(), resource.Tags);

        private static bool Apply(Photo photo, string category, string secureUrl, Dictionary<string, string>? context, string[]? tags)
        {
            var before = (photo.Category, photo.ImageUrl, photo.Title, photo.Description, photo.IsFeatured);

            photo.Category = category;
            photo.ImageUrl = secureUrl;
            photo.IsFeatured = tags?.Contains(FeaturedTag, StringComparer.OrdinalIgnoreCase) ?? false;

            if (context != null && context.TryGetValue("caption", out var caption) && !string.IsNullOrWhiteSpace(caption))
                photo.Title = Truncate(caption, 100);
            if (context != null && context.TryGetValue("alt", out var alt) && !string.IsNullOrWhiteSpace(alt))
                photo.Description = Truncate(alt, 500);

            return before != (photo.Category, photo.ImageUrl, photo.Title, photo.Description, photo.IsFeatured);
        }

        // Prefer the camera's EXIF capture date, fall back to the upload date
        private static DateTime DateTakenFrom(SearchResource asset)
        {
            if (asset.ImageMetadata != null
                && asset.ImageMetadata.TryGetValue("DateTimeOriginal", out var exif)
                && DateTime.TryParseExact(exif?.ToString(), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var taken))
                return taken;

            // The SDK hands CreatedAt back as local time without a zone, so convert it to UTC like the upload endpoint uses
            return DateTime.TryParse(asset.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var created)
                ? created
                : DateTime.UtcNow;
        }

        // "_DSC8604-8" -> "DSC8604 8", "golden_hour" -> "Golden Hour"
        private static string TitleFromFileName(string name)
        {
            var words = name.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? "Untitled"
                : string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        }

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
    }

    // Runs the sync on startup and then every Cloudinary:SyncIntervalMinutes (default 5)
    public class CloudinarySyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CloudinarySyncWorker> _logger;

        public CloudinarySyncWorker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CloudinarySyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_config["Cloudinary:CloudName"]))
            {
                _logger.LogWarning("Cloudinary is not configured; folder sync is disabled.");
                return;
            }

            var interval = TimeSpan.FromMinutes(_config.GetValue("Cloudinary:SyncIntervalMinutes", 5));
            var foldersCreated = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sync = scope.ServiceProvider.GetRequiredService<CloudinarySyncService>();

                    if (!foldersCreated)
                    {
                        await sync.EnsureFoldersAsync();
                        foldersCreated = true;
                    }

                    await sync.SyncAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Cloudinary sync failed; will retry in {Interval}", interval);
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
