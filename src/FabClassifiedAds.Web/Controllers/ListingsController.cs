using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

public class ListingsController(AppDbContext db, SearchService search) : Controller
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("/search")]
    [HttpGet("/c/{category}")]
    public async Task<IActionResult> Search(SearchFilters filters, string? category)
    {
        if (!string.IsNullOrEmpty(category)) filters.Category = category;

        var vm = new SearchPageViewModel { Filters = filters, Result = await search.SearchAsync(filters) };

        if (!string.IsNullOrEmpty(filters.Category))
        {
            vm.CurrentCategory = await db.Categories.AsNoTracking()
                .Include(c => c.Children.OrderBy(ch => ch.SortOrder))
                .FirstOrDefaultAsync(c => c.Slug == filters.Category);
            if (vm.CurrentCategory != null)
            {
                vm.ChildCategories = vm.CurrentCategory.Children;
                vm.Breadcrumb = await BuildBreadcrumbAsync(vm.CurrentCategory);
                var kinds = await GetSubtreeKindsAsync(vm.CurrentCategory.Id);
                vm.ShowCarFilters = kinds.Contains(CategoryKind.Cars);
                vm.ShowRealEstateFilters = kinds.Contains(CategoryKind.RealEstate);
            }
        }

        if (vm.ShowCarFilters || filters.HasCarFilters)
        {
            vm.ShowCarFilters = true;
            vm.Brands = await db.CarBrands.AsNoTracking().OrderByDescending(b => b.IsPopular).ThenBy(b => b.Name).ToListAsync();
            if (filters.BrandId.HasValue)
                vm.Models = await db.CarModels.AsNoTracking().Where(m => m.BrandId == filters.BrandId).OrderBy(m => m.Name).ToListAsync();
        }
        if (filters.HasRealEstateFilters) vm.ShowRealEstateFilters = true;

        if (UserId != null)
            vm.FavoriteIds = (await db.Favorites.Where(f => f.UserId == UserId).Select(f => f.ListingId).ToListAsync()).ToHashSet();

        return View(vm);
    }

    [HttpGet("/l/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var listing = await db.Listings
            .Include(l => l.Images.OrderBy(i => i.SortOrder))
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.Attributes)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Model)
            .Include(l => l.RealEstateDetails)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null) return NotFound();

        listing.ViewCount++;
        await db.SaveChangesAsync();

        var similar = await db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active && l.CategoryId == listing.CategoryId && l.Id != id)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .OrderByDescending(l => l.BumpedAt).Take(4).ToListAsync();

        var vm = new ListingDetailViewModel
        {
            Listing = listing,
            SimilarListings = similar,
            Breadcrumb = await BuildBreadcrumbAsync(listing.Category),
            IsOwner = UserId == listing.UserId,
            IsFavorite = UserId != null && await db.Favorites.AnyAsync(f => f.UserId == UserId && f.ListingId == id),
        };
        return View(vm);
    }

    [Authorize]
    [HttpGet("/post")]
    public async Task<IActionResult> Create()
    {
        return View(await PopulateAsync(new CreateListingViewModel()));
    }

    [Authorize]
    [HttpPost("/post")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateListingViewModel vm)
    {
        var category = await db.Categories.FindAsync(vm.CategoryId);
        if (category is null) ModelState.AddModelError(nameof(vm.CategoryId), "Please pick a category.");

        if (category?.Kind == CategoryKind.Cars)
        {
            if (vm.Year is null or < 1900) ModelState.AddModelError(nameof(vm.Year), "Year is required for cars.");
            if (vm.Mileage is null) ModelState.AddModelError(nameof(vm.Mileage), "Mileage is required for cars.");
        }
        if (category?.Kind == CategoryKind.RealEstate)
        {
            if (vm.SurfaceM2 is null or <= 0) ModelState.AddModelError(nameof(vm.SurfaceM2), "Surface is required for real estate.");
            if (vm.PropertyType is null) ModelState.AddModelError(nameof(vm.PropertyType), "Property type is required.");
            if (vm.Transaction is null) ModelState.AddModelError(nameof(vm.Transaction), "Transaction type is required.");
        }

        if (!ModelState.IsValid) return View(await PopulateAsync(vm));

        var listing = new Listing
        {
            Title = vm.Title.Trim(),
            Description = vm.Description.Trim(),
            Price = vm.IsFree ? null : vm.Price,
            Currency = vm.Currency,
            IsNegotiable = vm.IsNegotiable,
            IsFree = vm.IsFree,
            CategoryId = vm.CategoryId,
            City = vm.City.Trim(),
            Region = vm.Region.Trim(),
            Condition = vm.Condition,
            UserId = UserId!,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        listing.Images.Add(new ListingImage { Url = $"/media/ph/{CategorySeeder.Slugify(vm.Title)}-0.svg" });

        if (category!.Kind == CategoryKind.Cars)
        {
            listing.CarDetails = new CarDetails
            {
                BrandId = vm.BrandId, ModelId = vm.ModelId, Variant = vm.Variant,
                Year = vm.Year!.Value, Mileage = vm.Mileage!.Value,
                Fuel = vm.Fuel ?? FuelType.Petrol,
                Transmission = vm.Transmission ?? TransmissionType.Manual,
                Body = vm.Body ?? BodyType.Sedan,
                Drive = vm.Drive ?? Drivetrain.FrontWheel,
                EngineCc = vm.EngineCc, PowerHp = vm.PowerHp, Color = vm.Color,
                Options = vm.Options.Aggregate(CarOptions.None, (acc, o) => acc | o),
            };
        }
        else if (category.Kind == CategoryKind.RealEstate)
        {
            listing.RealEstateDetails = new RealEstateDetails
            {
                PropertyType = vm.PropertyType!.Value, Transaction = vm.Transaction!.Value,
                Rooms = vm.Rooms, SurfaceM2 = vm.SurfaceM2!.Value, Floor = vm.Floor,
                TotalFloors = vm.TotalFloors, YearBuilt = vm.YearBuilt,
                Heating = vm.Heating ?? HeatingType.None,
                Furnished = vm.Furnished ?? FurnishedState.Unfurnished,
                Amenities = vm.Amenities.Aggregate(PropertyAmenities.None, (acc, a) => acc | a),
            };
        }

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        TempData["Flash"] = "Your ad is live! 🎉";
        return RedirectToAction(nameof(Details), new { id = listing.Id });
    }

    /// <summary>JSON endpoint for the dependent brand -> model dropdown.</summary>
    [HttpGet("/api/car-models/{brandId:int}")]
    public async Task<IActionResult> CarModels(int brandId) =>
        Json(await db.CarModels.AsNoTracking()
            .Where(m => m.BrandId == brandId).OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name }).ToListAsync());

    private async Task<CreateListingViewModel> PopulateAsync(CreateListingViewModel vm)
    {
        vm.CategoryTree = await db.Categories.AsNoTracking()
            .Include(c => c.Children.OrderBy(ch => ch.SortOrder))
            .Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToListAsync();
        vm.Brands = await db.CarBrands.AsNoTracking().OrderByDescending(b => b.IsPopular).ThenBy(b => b.Name).ToListAsync();
        return vm;
    }

    private async Task<List<Category>> BuildBreadcrumbAsync(Category category)
    {
        var crumbs = new List<Category> { category };
        var current = category;
        while (current.ParentId.HasValue)
        {
            current = await db.Categories.AsNoTracking().FirstAsync(c => c.Id == current.ParentId);
            crumbs.Insert(0, current);
        }
        return crumbs;
    }

    private async Task<HashSet<CategoryKind>> GetSubtreeKindsAsync(int categoryId)
    {
        var ids = await search.GetCategoryWithDescendantsAsync(
            (await db.Categories.FindAsync(categoryId))!.Slug);
        return (await db.Categories.Where(c => ids.Contains(c.Id)).Select(c => c.Kind).Distinct().ToListAsync()).ToHashSet();
    }
}
