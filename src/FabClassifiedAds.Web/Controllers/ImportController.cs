using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Models.Import;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Ingestion API for externally-collected listings. Every write is guarded by an
/// API key (header <c>X-Import-Key</c>, value from config <c>Import:ApiKey</c> or the
/// <c>IMPORT_API_KEY</c> env var). Imported rows land in a staging table and are NOT
/// public until a human promotes them via <c>/publish</c> — that review step is where
/// you enforce that you actually have the right to republish a given item.
/// </summary>
[ApiController]
[Route("api/import")]
public class ImportController(
    AppDbContext db,
    IConfiguration config,
    UserManager<ApplicationUser> userManager,
    ILogger<ImportController> logger) : ControllerBase
{
    private const string SystemUserEmail = "import@aicigasesti.local";

    private bool KeyOk()
    {
        var expected = config["Import:ApiKey"] ?? Environment.GetEnvironmentVariable("IMPORT_API_KEY");
        if (string.IsNullOrEmpty(expected)) return false;                 // not configured => locked
        var provided = Request.Headers["X-Import-Key"].ToString();
        if (string.IsNullOrEmpty(provided)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }

    [HttpPost("listings")]
    public async Task<IActionResult> Ingest([FromBody] ImportBatchDto batch)
    {
        if (!KeyOk()) return Unauthorized();
        var result = new ImportResult { Received = batch.Listings.Count };

        foreach (var dto in batch.Listings)
        {
            var source = (dto.Source ?? batch.Source ?? "unknown").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(dto.ExternalId) || string.IsNullOrWhiteSpace(dto.Title))
            {
                result.Errors.Add($"{source}/{dto.ExternalId}: missing ExternalId or Title");
                continue;
            }

            var imagesJson = dto.Images is { Count: > 0 } ? JsonSerializer.Serialize(dto.Images) : null;
            var attrsJson = dto.Attributes is { Count: > 0 } ? JsonSerializer.Serialize(dto.Attributes) : null;
            var hash = ContentHash(dto, imagesJson, attrsJson);

            var existing = await db.ImportedListings
                .FirstOrDefaultAsync(i => i.Source == source && i.ExternalId == dto.ExternalId);

            if (existing is null)
            {
                db.ImportedListings.Add(new ImportedListing
                {
                    Source = source,
                    ExternalId = dto.ExternalId,
                    SourceUrl = dto.SourceUrl,
                    Title = dto.Title,
                    Description = dto.Description,
                    Price = dto.Price,
                    Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency!,
                    City = dto.City,
                    Region = dto.Region,
                    CategorySlug = dto.CategorySlug,
                    ImagesJson = imagesJson,
                    AttributesJson = attrsJson,
                    ContentHash = hash,
                });
                result.Inserted++;
            }
            else if (existing.ContentHash != hash)
            {
                existing.SourceUrl = dto.SourceUrl;
                existing.Title = dto.Title;
                existing.Description = dto.Description;
                existing.Price = dto.Price;
                existing.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency!;
                existing.City = dto.City;
                existing.Region = dto.Region;
                existing.CategorySlug = dto.CategorySlug;
                existing.ImagesJson = imagesJson;
                existing.AttributesJson = attrsJson;
                existing.ContentHash = hash;
                existing.UpdatedAt = DateTime.UtcNow;
                // a changed source row that was already published goes back to review
                if (existing.Status == ImportStatus.Published) existing.Status = ImportStatus.Pending;
                result.Updated++;
            }
            else
            {
                result.Unchanged++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Import batch: received {Received}, inserted {Inserted}, updated {Updated}",
            result.Received, result.Inserted, result.Updated);
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending([FromQuery] int take = 50, [FromQuery] string? source = null)
    {
        if (!KeyOk()) return Unauthorized();
        var q = db.ImportedListings.AsNoTracking().Where(i => i.Status == ImportStatus.Pending);
        if (!string.IsNullOrWhiteSpace(source)) q = q.Where(i => i.Source == source.ToLowerInvariant());
        var items = await q.OrderByDescending(i => i.UpdatedAt).Take(Math.Clamp(take, 1, 500)).ToListAsync();
        return Ok(items);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        if (!KeyOk()) return Unauthorized();
        var byStatus = await db.ImportedListings.GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync();
        var bySource = await db.ImportedListings.GroupBy(i => i.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() }).ToListAsync();
        return Ok(new { byStatus, bySource });
    }

    /// <summary>
    /// Promote a staged row to a real listing. Defaults to <see cref="ListingStatus.Draft"/>
    /// (invisible) so a human can review; pass <c>?activate=true</c> to publish live.
    /// </summary>
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, [FromQuery] bool activate = false)
    {
        if (!KeyOk()) return Unauthorized();

        var staged = await db.ImportedListings.FirstOrDefaultAsync(i => i.Id == id);
        if (staged is null) return NotFound();

        var category = string.IsNullOrWhiteSpace(staged.CategorySlug)
            ? null
            : await db.Categories.FirstOrDefaultAsync(c => c.Slug == staged.CategorySlug);
        if (category is null)
            return BadRequest($"Unknown or missing CategorySlug '{staged.CategorySlug}'. Set a valid category before publishing.");

        var systemUserId = await GetOrCreateSystemUserAsync();

        var listing = new Listing
        {
            Title = staged.Title.Trim(),
            Description = (staged.Description ?? "").Trim(),
            Price = staged.Price,
            Currency = staged.Currency,
            CategoryId = category.Id,
            City = staged.City ?? "",
            Region = staged.Region ?? "",
            UserId = systemUserId,
            SellerType = SellerType.Business,
            Status = activate ? ListingStatus.Active : ListingStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };

        if (!string.IsNullOrEmpty(staged.ImagesJson))
        {
            var urls = JsonSerializer.Deserialize<List<string>>(staged.ImagesJson) ?? [];
            for (var i = 0; i < urls.Count; i++)
                listing.Images.Add(new ListingImage { Url = urls[i], SortOrder = i });
        }
        // keep provenance for transparency / takedown handling
        if (!string.IsNullOrEmpty(staged.SourceUrl))
            listing.Attributes.Add(new ListingAttribute { Key = "source", Value = staged.SourceUrl });

        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        staged.Status = ImportStatus.Published;
        staged.PublishedListingId = listing.Id;
        staged.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { staged.Id, listingId = listing.Id, listing.Status });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        if (!KeyOk()) return Unauthorized();
        var staged = await db.ImportedListings.FirstOrDefaultAsync(i => i.Id == id);
        if (staged is null) return NotFound();
        staged.Status = ImportStatus.Rejected;
        staged.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { staged.Id, staged.Status });
    }

    private async Task<string> GetOrCreateSystemUserAsync()
    {
        var user = await userManager.FindByEmailAsync(SystemUserEmail);
        if (user != null) return user.Id;
        user = new ApplicationUser
        {
            UserName = SystemUserEmail, Email = SystemUserEmail, EmailConfirmed = true,
            DisplayName = "Import", IsBusiness = true, AvatarColor = "#6d5dfc",
        };
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
            throw new InvalidOperationException("Could not create the import system user: " +
                string.Join("; ", created.Errors.Select(e => e.Description)));
        return user.Id;
    }

    private static string ContentHash(ImportListingDto dto, string? imagesJson, string? attrsJson)
    {
        var payload = string.Join("|",
            dto.Title, dto.Description, dto.Price, dto.Currency, dto.City, dto.Region,
            dto.CategorySlug, dto.SourceUrl, imagesJson, attrsJson);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..32];
    }
}
