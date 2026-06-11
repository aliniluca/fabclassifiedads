namespace FabClassifiedAds.Web.Services;

/// <summary>Stores uploaded listing photos under wwwroot/uploads and returns their public URLs.</summary>
public class PhotoStorage(IWebHostEnvironment env)
{
    public const int MaxPhotos = 10;
    public const long MaxBytesPerPhoto = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public string? Validate(IReadOnlyCollection<IFormFile> photos)
    {
        if (photos.Count > MaxPhotos) return $"You can upload at most {MaxPhotos} photos.";
        foreach (var photo in photos)
        {
            if (photo.Length == 0) return "One of the photos is empty.";
            if (photo.Length > MaxBytesPerPhoto) return $"\"{photo.FileName}\" is over 5 MB.";
            if (!AllowedTypes.ContainsKey(photo.ContentType)) return $"\"{photo.FileName}\" is not a supported image type (JPEG, PNG, WebP, GIF).";
        }
        return null;
    }

    public async Task<List<string>> SaveAsync(IEnumerable<IFormFile> photos, CancellationToken ct = default)
    {
        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);

        var urls = new List<string>();
        foreach (var photo in photos)
        {
            var name = $"{Guid.NewGuid():N}{AllowedTypes[photo.ContentType]}";
            await using var stream = File.Create(Path.Combine(dir, name));
            await photo.CopyToAsync(stream, ct);
            urls.Add($"/uploads/{name}");
        }
        return urls;
    }
}
