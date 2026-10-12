using System.Net;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Serves deterministic gradient SVG placeholders so demo listings always have imagery
/// without external dependencies, plus 9:16 share cards for TikTok/Instagram stories.
/// </summary>
public class MediaController(AppDbContext db, IWebHostEnvironment env) : Controller
{
    private static readonly (string From, string To)[] Palettes =
    [
        ("#667eea", "#764ba2"), ("#f093fb", "#f5576c"), ("#4facfe", "#00f2fe"),
        ("#43e97b", "#38f9d7"), ("#fa709a", "#fee140"), ("#30cfd0", "#330867"),
        ("#a8edea", "#fed6e3"), ("#ff9a9e", "#fecfef"), ("#fbc2eb", "#a6c1ee"),
        ("#fdcbf1", "#e6dee9"), ("#a1c4fd", "#c2e9fb"), ("#d4fc79", "#96e6a1"),
        ("#84fab0", "#8fd3f4"), ("#cfd9df", "#e2ebf0"), ("#fccb90", "#d57eeb"),
        ("#e0c3fc", "#8ec5fc"), ("#f5f7fa", "#c3cfe2"), ("#fad0c4", "#ffd1ff"),
    ];

    [HttpGet("/media/ph/{seed}.svg")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Placeholder(string seed)
    {
        // default "no photo" plate for every ad without pictures: flat field, hairline frame,
        // the AiciGăsești stamp mark and wordmark in muted ink
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
              <rect width="800" height="600" fill="#e9edf1"/>
              <g transform="translate(352 196) scale(3)" fill="none" stroke="#7d8a99" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="16" cy="16" r="14" stroke-width="2.2"/>
                <circle cx="16" cy="16" r="10.5" stroke-width="1"/>
                <path d="m11 16.4 3.4 3.4 6.6-7.2" stroke-width="2.4"/>
              </g>
              <text x="400" y="358" text-anchor="middle" fill="#7d8a99" font-size="46" letter-spacing="-0.5"
                    font-family="'Public Sans', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"><tspan font-weight="700">Aici</tspan><tspan font-weight="400">Găsești</tspan></text>
            </svg>
            """;
        return Content(svg, "image/svg+xml");
    }

    /// <summary>9:16 share card (1080x1920) for posting a listing to TikTok / Instagram stories.</summary>
    [HttpGet("/l/{id:int}/share-card.svg")]
    public async Task<IActionResult> ShareCard(int id)
    {
        var listing = await db.Listings.AsNoTracking()
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();

        var hash = Math.Abs(listing.Title.GetHashCode(StringComparison.Ordinal));
        var (from, to) = Palettes[hash % Palettes.Length];

        // embed the cover photo when it is a real uploaded file; otherwise keep the gradient
        var photoElement = "";
        var imageUrl = listing.Images.FirstOrDefault()?.Url;
        if (imageUrl != null && imageUrl.StartsWith("/uploads/"))
        {
            var path = Path.Combine(env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path) && new FileInfo(path).Length < 8 * 1024 * 1024)
            {
                var mime = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg",
                };
                var data = Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(path));
                photoElement = $"""<image href="data:{mime};base64,{data}" x="60" y="320" width="960" height="960" preserveAspectRatio="xMidYMid slice" clip-path="url(#photo)"/>""";
            }
        }

        var titleLines = WrapText(listing.Title, 26).Take(3).ToList();
        var titleSvg = string.Join("", titleLines.Select((line, i) =>
            $"""<text x="80" y="{1450 + i * 78}" font-family="Arial, sans-serif" font-size="62" font-weight="bold" fill="#ffffff">{WebUtility.HtmlEncode(line)}</text>"""));

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1080" height="1920" viewBox="0 0 1080 1920">
              <defs>
                <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stop-color="#0e0f17"/>
                  <stop offset="55%" stop-color="#171927"/>
                  <stop offset="100%" stop-color="{from}"/>
                </linearGradient>
                <linearGradient id="accent" x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stop-color="{from}"/>
                  <stop offset="100%" stop-color="{to}"/>
                </linearGradient>
                <clipPath id="photo"><rect x="60" y="320" width="960" height="960" rx="48"/></clipPath>
              </defs>
              <rect width="1080" height="1920" fill="url(#bg)"/>
              <text x="80" y="170" font-family="Arial, sans-serif" font-size="76" font-weight="bold" fill="#ffffff">Aici<tspan fill="{to}">Găsești</tspan></text>
              <text x="80" y="245" font-family="Arial, sans-serif" font-size="40" fill="#9aa0bd">{WebUtility.HtmlEncode(listing.Category.Icon + " " + listing.Category.Name)}</text>
              <rect x="60" y="320" width="960" height="960" rx="48" fill="url(#accent)" opacity="0.85"/>
              {photoElement}
              {titleSvg}
              <text x="80" y="{1480 + titleLines.Count * 78}" font-family="Arial, sans-serif" font-size="44" fill="#cfd3e8">{WebUtility.HtmlEncode(listing.ListingSummary())}</text>
              <text x="80" y="{1590 + titleLines.Count * 78}" font-family="Arial, sans-serif" font-size="96" font-weight="bold" fill="url(#accent)">{WebUtility.HtmlEncode(listing.Price())}</text>
              <text x="80" y="1850" font-family="Arial, sans-serif" font-size="40" fill="#9aa0bd">{WebUtility.HtmlEncode(listing.City)}   ·   aicigasesti.ro/l/{listing.Id}</text>
            </svg>
            """;
        return Content(svg, "image/svg+xml");
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length + word.Length + 1 > maxChars && line.Length > 0)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = line.Length == 0 ? word : $"{line} {word}";
            }
        }
        if (line.Length > 0) yield return line;
    }
}
