using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            RootCategories = await db.Categories.AsNoTracking()
                .Where(c => c.ParentId == null)
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder))
                .OrderBy(c => c.SortOrder).ToListAsync(),
            FeaturedListings = await ActiveListings()
                .Where(l => l.IsFeatured)
                .OrderByDescending(l => l.BumpedAt).Take(8).ToListAsync(),
            LatestListings = await ActiveListings()
                .OrderByDescending(l => l.CreatedAt).Take(12).ToListAsync(),
            TotalActiveListings = await db.Listings.CountAsync(l => l.Status == ListingStatus.Active),
            TotalUsers = await db.Users.CountAsync(),
            TotalCategories = await db.Categories.CountAsync(),
            PopularBrands = await db.CarBrands.AsNoTracking()
                .Where(b => b.IsPopular).OrderBy(b => b.Name).ToListAsync(),
        };
        return View(vm);
    }

    private IQueryable<Listing> ActiveListings() =>
        db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails);

    [Route("/error")]
    public IActionResult Error() => View();

    [Route("/access-denied")]
    public IActionResult AccessDenied() => View();
}
