using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Components;

/// <summary>Renders the live advertising banners for a placement and counts impressions.</summary>
public class BannerViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(BannerPlacement placement)
    {
        var now = DateTime.UtcNow;
        var banners = await db.Banners.AsNoTracking()
            .Where(b => b.Placement == placement && b.IsActive
                && (b.StartsAt == null || b.StartsAt <= now)
                && (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.SortOrder)
            .Take(3)
            .ToListAsync();

        if (banners.Count > 0)
        {
            // ids are integer primary keys from the DB (never user input) — safe to inline
            var ids = string.Join(",", banners.Select(b => b.Id));
#pragma warning disable EF1002 // interpolated values are trusted integer ids
            await db.Database.ExecuteSqlRawAsync($"UPDATE \"Banners\" SET \"Impressions\" = \"Impressions\" + 1 WHERE \"Id\" IN ({ids})");
#pragma warning restore EF1002
        }
        return View(banners);
    }
}
