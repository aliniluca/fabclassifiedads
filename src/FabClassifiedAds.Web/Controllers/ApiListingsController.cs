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
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string ApiUserEmail = "api@aicigasesti.local";

    /// <summary>GET /api/categories — list category slugs (for choosing CategorySlug).</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var cats = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Slug, c.Name, c.Kind, Parent = c.Parent!.Slug })
            .ToListAsync();
        return Ok(cats);
    }

    /// <summary>POST /api/listings — create a listing. Auto-detects the category when omitted.</summary>
    [HttpPost("listings")]
    public async Task<IActionResult> Create([FromBody] CreateListingApiDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // resolve category: explicit slug, else auto-detect from the text
        var slug = dto.CategorySlug;
        int? detectedBrandId = null, detectedModelId = null;
        if (string.IsNullOrWhiteSpace(slug))
        {
            var guess = await detector.DetectAsync(dto.Title, dto.Description);
            slug = guess.Slug;
            detectedBrandId = guess.BrandId;
            detectedModelId = guess.ModelId;
        }
        if (string.IsNullOrWhiteSpace(slug))
            return Problem(statusCode: 422, title: "Could not determine a category. Pass CategorySlug (see GET /api/categories).");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (category is null)
            return Problem(statusCode: 400, title: $"Unknown CategorySlug '{slug}'.");

        // per-kind required fields
        if (category.Kind == CategoryKind.Cars && dto.Car is null)
            return Problem(statusCode: 400, title: "This category needs a 'car' object (year, mileage, …).");
        if (category.Kind == CategoryKind.RealEstate && dto.RealEstate is null)
            return Problem(statusCode: 400, title: "This category needs a 'realEstate' object (surfaceM2, propertyType, …).");

        var ownerId = await GetOrCreateApiUserAsync();
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
            SellerType = SellerType.Business,
            UserId = ownerId,
            Status = dto.Active ? ListingStatus.Active : ListingStatus.Draft,
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

    private async Task<string> GetOrCreateApiUserAsync()
    {
        var user = await userManager.FindByEmailAsync(ApiUserEmail);
        if (user != null) return user.Id;
        user = new ApplicationUser
        {
            UserName = ApiUserEmail, Email = ApiUserEmail, EmailConfirmed = true,
            DisplayName = "AiciGăsești API", IsBusiness = true, AvatarColor = "#6d5dfc",
        };
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException("Could not create API user: " +
                string.Join("; ", created.Errors.Select(e => e.Description)));
        return user.Id;
    }
}
