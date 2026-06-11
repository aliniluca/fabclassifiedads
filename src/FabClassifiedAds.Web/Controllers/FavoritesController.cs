using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

[Authorize]
public class FavoritesController(AppDbContext db) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/favorites")]
    public async Task<IActionResult> Index()
    {
        var favorites = await db.Favorites.AsNoTracking()
            .Where(f => f.UserId == UserId)
            .Include(f => f.Listing).ThenInclude(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(f => f.Listing).ThenInclude(l => l.Category)
            .Include(f => f.Listing).ThenInclude(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(f => f.Listing).ThenInclude(l => l.RealEstateDetails)
            .OrderByDescending(f => f.CreatedAt).ToListAsync();
        return View(favorites);
    }

    [HttpPost("/favorites/toggle/{listingId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int listingId)
    {
        var existing = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == UserId && f.ListingId == listingId);
        bool isFavorite;
        if (existing != null)
        {
            db.Favorites.Remove(existing);
            isFavorite = false;
        }
        else
        {
            if (!await db.Listings.AnyAsync(l => l.Id == listingId)) return NotFound();
            db.Favorites.Add(new Favorite { UserId = UserId, ListingId = listingId });
            isFavorite = true;
        }
        await db.SaveChangesAsync();
        if (Request.Headers.XRequestedWith == "fetch") return Json(new { isFavorite });
        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/");
    }
}
