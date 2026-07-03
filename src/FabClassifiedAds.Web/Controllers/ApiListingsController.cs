using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Api;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Token-protected API for creating listings programmatically. See API.md.
/// Every request must carry the shared secret in the <c>X-Api-Key</c> header.
/// </summary>
[ApiController]
[Route("api")]
[ApiKey]
public class ApiListingsController(
    AppDbContext db,
    PhotoStorage photos,
    SafeHttpFetcher fetcher,
    CategoryDetector detector,
    TrustService trust,
    ContentModerationService moderation,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string ApiUserEmail = "api@aicigasesti.local";

    /// <summary>GET /api/categories — every category with id, slug, kind, parent and the
    /// nested object it requires ("car", "realEstate", or null). Use it to map external
    /// categories to ours.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var cats = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Slug, c.Name, c.Kind, Parent = c.Parent!.Slug })
            .ToListAsync();

        return Ok(cats.Select(c => new
        {
            c.Id,
            c.Slug,
            c.Name,
            kind = c.Kind.ToString(),
            parent = c.Parent,
            requires = c.Kind switch
            {
                CategoryKind.Cars => "car",
                CategoryKind.RealEstate => "realEstate",
                _ => (string?)null,
            },
        }));
    }

    /// <summary>POST /api/listings — create a listing. Auto-detects the category when omitted.</summary>
    [HttpPost("listings")]
    public async Task<IActionResult> Create([FromBody] CreateListingApiDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // resolve category: explicit id, then slug, then auto-detect from the text
        int? detectedBrandId = null, detectedModelId = null;
        Category? category = null;

        if (dto.CategoryId is int cid)
        {
            category = await db.Categories.FindAsync(cid);
            if (category is null) return Problem(statusCode: 400, title: $"Unknown CategoryId {cid}.");
        }
        if (category is null && !string.IsNullOrWhiteSpace(dto.CategorySlug))
        {
            category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == dto.CategorySlug);
            if (category is null) return Problem(statusCode: 400, title: $"Unknown CategorySlug '{dto.CategorySlug}'.");
        }
        if (category is null)
        {
            var guess = await detector.DetectAsync(dto.Title, dto.Description);
            detectedBrandId = guess.BrandId;
            detectedModelId = guess.ModelId;
            if (guess.CategoryId is int gid) category = await db.Categories.FindAsync(gid);
        }
        if (category is null)
            return Problem(statusCode: 422, title: "Could not determine a category. Pass categorySlug or categoryId (see GET /api/categories).");

        // per-kind required fields
        if (category.Kind == CategoryKind.Cars && dto.Car is null)
            return Problem(statusCode: 400, title: "This category needs a 'car' object (year, mileage, …).");
        if (category.Kind == CategoryKind.RealEstate && dto.RealEstate is null)
            return Problem(statusCode: 400, title: "This category needs a 'realEstate' object (surfaceM2, propertyType, …).");

        // owner: the account whose key was used, or the system API account for the master key
        var apiUserId = HttpContext.Items[ApiKeyAttribute.UserIdItem] as string;
        var owner = apiUserId is null
            ? await GetOrCreateApiUserAsync()
            : await userManager.FindByIdAsync(apiUserId);
        if (owner is null) return Problem(statusCode: 401, title: "API key owner not found.");

        // moderation overrides the requested status: flagged content is always held
        var verdict = moderation.Analyze(dto.Title, dto.Description);
        var status = verdict.NeedsReview
            ? ListingStatus.PendingReview
            : dto.Active ? ListingStatus.Active : ListingStatus.Draft;

        var listing = new Listing
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.IsFree ? null : dto.Price,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency,
            IsNegotiable = dto.IsNegotiable,
            IsFree = dto.IsFree,
            CategoryId = category.Id,
            City = dto.City.Trim(),
            Region = dto.Region?.Trim() ?? "",
            Condition = dto.Condition,
            SellerType = owner.IsBusiness ? SellerType.Business : SellerType.Private,
            UserId = owner.Id,
            Status = status,
            ModerationNote = verdict.Note,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };

        if (category.Kind == CategoryKind.Cars && dto.Car is { } car)
        {
            listing.CarDetails = new CarDetails
            {
                BrandId = car.BrandId ?? detectedBrandId,
                ModelId = car.ModelId ?? detectedModelId,
                Variant = car.Variant,
                Year = car.Year,
                Mileage = car.Mileage,
                Fuel = car.Fuel,
                Transmission = car.Transmission,
                Body = car.Body,
                Drive = car.Drive,
                EngineCc = car.EngineCc,
                PowerHp = car.PowerHp,
                Color = car.Color,
                Options = (car.Options ?? []).Aggregate(CarOptions.None, (a, o) => a | o),
            };
        }
        else if (category.Kind == CategoryKind.RealEstate && dto.RealEstate is { } re)
        {
            listing.RealEstateDetails = new RealEstateDetails
            {
                PropertyType = re.PropertyType,
                Transaction = re.Transaction,
                Rooms = re.Rooms,
                SurfaceM2 = re.SurfaceM2,
                Floor = re.Floor,
                TotalFloors = re.TotalFloors,
                YearBuilt = re.YearBuilt,
                Heating = re.Heating,
                Furnished = re.Furnished,
                Amenities = (re.Amenities ?? []).Aggregate(PropertyAmenities.None, (a, x) => a | x),
            };
        }

        await AttachImagesAsync(listing, dto.Images);

        if (GeoData.Locate(listing.City, Environment.TickCount) is { } coords)
        {
            listing.Latitude = coords.Lat;
            listing.Longitude = coords.Lng;
        }

        listing.TrustScore = (await trust.AnalyzeListingAsync(listing)).Score;

        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var url = $"{Request.Scheme}://{Request.Host}/l/{listing.Id}";
        return Created(url, new
        {
            id = listing.Id,
            url,
            status = listing.Status.ToString(),
            categorySlug = category.Slug,
            trustScore = listing.TrustScore,
        });
    }

    private async Task AttachImagesAsync(Listing listing, List<string>? urls)
    {
        var order = 0;
        foreach (var remote in (urls ?? []).Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            if (order >= PhotoStorage.MaxPhotos) break;
            try
            {
                if (await fetcher.FetchImageAsync(remote) is { } dl &&
                    await photos.SaveDownloadedAsync(dl.Data, dl.ContentType) is { } saved)
                    listing.Images.Add(new ListingImage { Url = saved, SortOrder = order++ });
            }
            catch { /* skip an unfetchable image */ }
        }
        if (listing.Images.Count == 0)
            listing.Images.Add(new ListingImage { Url = $"/media/ph/{CategorySeeder.Slugify(listing.Title)}-0.svg" });
    }

    private async Task<ApplicationUser> GetOrCreateApiUserAsync()
    {
        var user = await userManager.FindByEmailAsync(ApiUserEmail);
        if (user != null) return user;
        user = new ApplicationUser
        {
            UserName = ApiUserEmail, Email = ApiUserEmail, EmailConfirmed = true,
            DisplayName = "AiciGăsești", IsBusiness = false, AvatarColor = "#6d5dfc",
        };
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException("Could not create API user: " +
                string.Join("; ", created.Errors.Select(e => e.Description)));
        return user;
    }
}
