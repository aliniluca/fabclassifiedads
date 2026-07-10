using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>Public seller profile — the seller's header + all their active listings (like a dealer inventory).</summary>
public class ProfileController(AppDbContext db, TrustService trust) : Controller
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private const int PageSize = 24;

    [HttpGet("/u/{id}/{slug?}")]
    public async Task<IActionResult> Index(string id, SortOption sort = SortOption.Newest, int page = 1)
    {
        var seller = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (seller is null) return NotFound();

        var baseQuery = db.Listings.AsNoTracking()
            .Where(l => l.UserId == id && l.Status == ListingStatus.Active);

        var total = await baseQuery.CountAsync();

        var query = baseQuery
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .AsQueryable();

        query = sort switch
        {
            SortOption.PriceAsc => query.OrderBy(l => l.Price == null).ThenBy(l => l.Price),
            SortOption.PriceDesc => query.OrderByDescending(l => l.Price),
            SortOption.MostViewed => query.OrderByDescending(l => l.ViewCount),
            _ => query.OrderByDescending(l => l.BumpedAt),
        };

        page = Math.Max(1, page);
        var listings = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var favoriteIds = UserId == null
            ? []
            : (await db.Favorites.Where(f => f.UserId == UserId).Select(f => f.ListingId).ToListAsync()).ToHashSet();

        var vm = new SellerProfileViewModel
        {
            Seller = seller,
            Listings = listings,
            TotalActive = total,
            Page = page,
            PageSize = PageSize,
            Sort = sort,
            Trust = await trust.AnalyzeSellerAsync(id),
            FavoriteIds = favoriteIds,
            IsSelf = UserId == id,
        };
        return View(vm);
    }
}
