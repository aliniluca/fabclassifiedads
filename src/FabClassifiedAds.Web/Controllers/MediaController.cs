using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Serves the default "no photo" plate (AiciGăsești mark) and the 9:16 share cards
/// for TikTok/Instagram stories.
/// </summary>
public class MediaController(AppDbContext db) : Controller
{
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

    /// <summary>9:16 share card (1080x1920 JPEG) for posting a listing to TikTok / Instagram stories.</summary>
    [HttpGet("/l/{id:int}/share-card.jpg")]
    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> ShareCard(int id, [FromServices] ShareCardRenderer renderer)
    {
        var listing = await db.Listings.AsNoTracking()
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .FirstOrDefaultAsync(l => l.Id == id);
        // same visibility rule as the ad page: no cards for pending/rejected/hidden ads
        if (listing is null || listing.Status is not (ListingStatus.Active or ListingStatus.Sold)) return NotFound();

        var category = Translator.IsRo ? Translator.RoText(listing.Category.Name) : listing.Category.Name;
        var bytes = await renderer.RenderAsync(listing, category, $"{Request.Host.Host}/l/{listing.Id}");
        return File(bytes, "image/jpeg");
    }

    /// <summary>Old SVG card links (shared before the JPEG version) keep working.</summary>
    [HttpGet("/l/{id:int}/share-card.svg")]
    public IActionResult LegacyShareCard(int id) => RedirectPermanent($"/l/{id}/share-card.jpg");
}
