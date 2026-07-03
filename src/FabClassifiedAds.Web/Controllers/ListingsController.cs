using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

public class ListingsController(
    AppDbContext db,
    SearchService search,
    PhotoStorage photos,
    TrustService trust,
    ListingUrlImporter urlImporter,
    SafeHttpFetcher fetcher,
    CategoryDetector categoryDetector) : Controller
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
            Trust = await trust.AnalyzeListingAsync(listing),
            SellerTrust = await trust.AnalyzeSellerAsync(listing.UserId),
        };
        return View(vm);
    }

    [Authorize]
    [HttpGet("/post")]
    public async Task<IActionResult> Create()
    {
        return View(await PopulateAsync(new CreateListingViewModel()));
    }

    /// <summary>
    /// Cross-post bridge: the signed-in owner pastes a link to their own ad elsewhere;
    /// we fetch that single page and return pre-fill data for the post form. Fetching is
    /// SSRF-guarded and reads only publisher metadata (schema.org / OpenGraph).
    /// </summary>
    [Authorize]
    [HttpPost("/post/import-url")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportUrl([FromForm] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !fetcher.IsFetchableUrl(url))
            return Json(new { ok = false, error = "Please paste a full http(s) link." });

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var prefill = await urlImporter.ImportAsync(url, cts.Token);
            if (prefill is null || string.IsNullOrWhiteSpace(prefill.Title))
                return Json(new { ok = false, error = "Couldn't read that page. Fill the form in manually." });

            var suggestion = await categoryDetector.DetectAsync(prefill.Title, prefill.Description);

            return Json(new
            {
                ok = true,
                data = new
                {
                    title = prefill.Title,
                    description = prefill.Description,
                    price = prefill.Price,
                    currency = prefill.Currency,
                    city = prefill.City,
                    region = prefill.Region,
                    images = prefill.Images,
                    categoryId = suggestion.CategoryId,
                    brandId = suggestion.BrandId,
                }
            });
        }
        catch
        {
            return Json(new { ok = false, error = "Couldn't reach that link. Fill the form in manually." });
        }
    }

    [Authorize]
    [HttpPost("/post")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(120_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 120_000_000)]
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

        var uploadedPhotos = (vm.Photos ?? []).Where(p => p.Length > 0).ToList();
        if (photos.Validate(uploadedPhotos) is { } photoError)
            ModelState.AddModelError(nameof(vm.Photos), photoError);
        if (vm.Video is { Length: > 0 } && photos.ValidateVideo(vm.Video) is { } videoError)
            ModelState.AddModelError(nameof(vm.Video), videoError);

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
        var imageUrls = new List<string>();
        if (uploadedPhotos.Count > 0)
            imageUrls.AddRange(await photos.SaveAsync(uploadedPhotos));
        // cross-posted images: download server-side (SSRF-guarded) up to the photo cap
        foreach (var remote in (vm.ImportedImageUrls ?? []).Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            if (imageUrls.Count >= PhotoStorage.MaxPhotos) break;
            try
            {
                if (await fetcher.FetchImageAsync(remote) is var img && img is { } dl &&
                    await photos.SaveDownloadedAsync(dl.Data, dl.ContentType) is { } saved)
                    imageUrls.Add(saved);
            }
            catch { /* skip an image that won't download; not fatal to publishing */ }
        }

        if (imageUrls.Count > 0)
            for (var i = 0; i < imageUrls.Count; i++)
                listing.Images.Add(new ListingImage { Url = imageUrls[i], SortOrder = i });
        else
            listing.Images.Add(new ListingImage { Url = $"/media/ph/{CategorySeeder.Slugify(vm.Title)}-0.svg" });

        if (vm.Video is { Length: > 0 })
            listing.VideoUrl = await photos.SaveVideoAsync(vm.Video);

        if (GeoData.Locate(vm.City, Environment.TickCount) is { } coords)
        {
            listing.Latitude = coords.Lat;
            listing.Longitude = coords.Lng;
        }

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

        listing.TrustScore = (await trust.AnalyzeListingAsync(listing)).Score;

        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        TempData["Flash"] = "Your ad is live! 🎉";
        return RedirectToAction(nameof(Details), new { id = listing.Id });
    }

    /// <summary>Vertical swipe feed — listings with video first, then featured and fresh ones.</summary>
    [HttpGet("/feed")]
    public async Task<IActionResult> Feed()
    {
        var items = await db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .OrderByDescending(l => l.VideoUrl != null)
            .ThenByDescending(l => l.IsFeatured)
            .ThenByDescending(l => l.BumpedAt)
            .Take(30)
            .ToListAsync();

        var favoriteIds = UserId == null
            ? []
            : (await db.Favorites.Where(f => f.UserId == UserId).Select(f => f.ListingId).ToListAsync()).ToHashSet();
        ViewBag.FavoriteIds = favoriteIds;
        return View(items);
    }

    /// <summary>Full-page map view; reuses the same filters as the list view.</summary>
    [HttpGet("/map")]
    public async Task<IActionResult> Map(SearchFilters filters)
    {
        var vm = new SearchPageViewModel { Filters = filters };
        if (!string.IsNullOrEmpty(filters.Category))
            vm.CurrentCategory = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == filters.Category);
        return View(vm);
    }

    /// <summary>JSON endpoint feeding the Leaflet map with filtered, geocoded listings.</summary>
    [HttpGet("/api/map-listings")]
    public async Task<IActionResult> MapListings(SearchFilters filters)
    {
        filters.Page = 1;
        filters.PageSize = 500;
        var result = await search.SearchAsync(filters);
        var markers = result.Items
            .Where(l => l.Latitude.HasValue && l.Longitude.HasValue)
            .Select(l => new
            {
                l.Id,
                l.Title,
                Price = l.Price(),
                l.City,
                Lat = l.Latitude!.Value,
                Lng = l.Longitude!.Value,
                Image = l.Images.FirstOrDefault()?.Url,
                Summary = l.ListingSummary(),
            });
        return Json(markers);
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
