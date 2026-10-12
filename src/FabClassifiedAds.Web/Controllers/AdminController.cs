using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using FabClassifiedAds.Web.Models.Admin;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

[Authorize(Policy = AdminAccess.PolicyName)]
[Route("admin")]
public class AdminController(
    AppDbContext db,
    PhotoStorage photos,
    SafeHttpFetcher fetcher,
    SettingsStore settings,
    SearchService search) : Controller
{
    // ---------- moderation queue ----------

    private const int RiskyBelow = 40;

    /// <summary>
    /// Moderation table. Tabs per status (+ "risky" = live ads with a trust score under 40),
    /// free-text search (title, description, ad no., seller email/name), root category, sort, paging.
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string tab = "pending", string? q = null, string? category = null,
        string sort = "newest", int page = 1)
    {
        var today = DateTime.UtcNow.Date;
        var vm = new AdminListingsViewModel
        {
            Tab = tab, Q = q?.Trim(), Category = category, Sort = sort, Page = Math.Max(1, page),
            RootCategories = await db.Categories.AsNoTracking().Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToListAsync(),
            Counts = new AdminCounts
            {
                Pending = await db.Listings.CountAsync(l => l.Status == ListingStatus.PendingReview),
                Active = await db.Listings.CountAsync(l => l.Status == ListingStatus.Active),
                Rejected = await db.Listings.CountAsync(l => l.Status == ListingStatus.Rejected),
                Expired = await db.Listings.CountAsync(l => l.Status == ListingStatus.Expired),
                Suspended = await db.Listings.CountAsync(l => l.Status == ListingStatus.Suspended),
                Risky = await db.Listings.CountAsync(l => l.Status == ListingStatus.Active && l.TrustScore < RiskyBelow),
                All = await db.Listings.CountAsync(),
                NewToday = await db.Listings.CountAsync(l => l.CreatedAt >= today),
                Users = await db.Users.CountAsync(),
            },
        };

        var query = db.Listings.AsNoTracking();
        query = tab switch
        {
            "active" => query.Where(l => l.Status == ListingStatus.Active),
            "rejected" => query.Where(l => l.Status == ListingStatus.Rejected),
            "expired" => query.Where(l => l.Status == ListingStatus.Expired),
            "suspended" => query.Where(l => l.Status == ListingStatus.Suspended),
            "risky" => query.Where(l => l.Status == ListingStatus.Active && l.TrustScore < RiskyBelow),
            "all" => query,
            _ => query.Where(l => l.Status == ListingStatus.PendingReview),
        };

        if (!string.IsNullOrEmpty(vm.Q))
        {
            var term = vm.Q.TrimStart('#');
            if (int.TryParse(term, out var adId))
                query = query.Where(l => l.Id == adId);
            else
                query = query.Where(l => EF.Functions.Like(l.Title, $"%{term}%") || EF.Functions.Like(l.Description, $"%{term}%")
                    || EF.Functions.Like(l.User.Email!, $"%{term}%") || EF.Functions.Like(l.User.DisplayName, $"%{term}%"));
        }
        if (!string.IsNullOrEmpty(category))
        {
            var ids = await search.GetCategoryWithDescendantsAsync(category);
            query = query.Where(l => ids.Contains(l.CategoryId));
        }

        query = sort switch
        {
            "oldest" => query.OrderBy(l => l.CreatedAt),
            "trust" => query.OrderBy(l => l.TrustScore).ThenByDescending(l => l.CreatedAt),
            "views" => query.OrderByDescending(l => l.ViewCount),
            "price" => query.OrderByDescending(l => l.Price),
            _ => query.OrderByDescending(l => l.CreatedAt),
        };

        vm.TotalCount = await query.CountAsync();
        vm.Page = Math.Min(vm.Page, vm.TotalPages);
        vm.Listings = await query
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .Skip((vm.Page - 1) * vm.PageSize).Take(vm.PageSize)
            .ToListAsync();

        return View(vm);
    }

    /// <summary>Bulk moderation on the ticked rows: approve, reject (with reason), delete, feature, unfeature.</summary>
    [HttpPost("listings/bulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(int[] ids, string action, string? reason, string? returnUrl)
    {
        var back = returnUrl is { Length: > 0 } && Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin";
        if (ids.Length == 0)
        {
            TempData["Flash"] = "Select at least one ad.";
            return LocalRedirect(back);
        }

        var listings = await db.Listings.Where(l => ids.Contains(l.Id)).ToListAsync();
        switch (action)
        {
            case "approve":
                foreach (var l in listings) { l.Status = ListingStatus.Active; l.ModerationNote = null; l.BumpedAt = DateTime.UtcNow; l.ExpiresAt ??= DateTime.UtcNow.AddDays(30); }
                break;
            case "reject":
                foreach (var l in listings)
                {
                    l.Status = ListingStatus.Rejected;
                    if (!string.IsNullOrWhiteSpace(reason)) l.ModerationNote = $"Rejected by admin: {reason.Trim()}";
                }
                break;
            case "delete":
                db.Listings.RemoveRange(listings);
                break;
            case "feature":
                foreach (var l in listings) l.IsFeatured = true;
                break;
            case "unfeature":
                foreach (var l in listings) l.IsFeatured = false;
                break;
            default:
                TempData["Flash"] = "Unknown action.";
                return LocalRedirect(back);
        }
        await db.SaveChangesAsync();
        var done = action switch
        {
            "approve" => Translator.IsRo ? "aprobate" : "approved",
            "reject" => Translator.IsRo ? "respinse" : "rejected",
            "delete" => Translator.IsRo ? "șterse" : "deleted",
            "feature" => Translator.IsRo ? "promovate" : "featured",
            _ => Translator.IsRo ? "fără promovare" : "unfeatured",
        };
        TempData["Flash"] = Translator.IsRo ? $"Anunțuri {done}: {listings.Count}." : $"Ads {done}: {listings.Count}.";
        return LocalRedirect(back);
    }

    [HttpPost("listings/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();
        listing.Status = ListingStatus.Active;
        listing.ModerationNote = null;   // an old rejection note no longer applies
        listing.BumpedAt = DateTime.UtcNow;
        listing.ExpiresAt ??= DateTime.UtcNow.AddDays(30);
        await db.SaveChangesAsync();
        TempData["Flash"] = $"Approved: {listing.Title}";
        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/admin");
    }

    [HttpPost("listings/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();
        listing.Status = ListingStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(reason))
            listing.ModerationNote = $"Rejected by admin: {reason.Trim()}";
        await db.SaveChangesAsync();
        TempData["Flash"] = $"Rejected: {listing.Title}";
        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/admin");
    }

    // ---------- edit / delete ----------

    [HttpGet("listings/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var l = await db.Listings
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.CarDetails)
            .Include(x => x.RealEstateDetails)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();

        var vm = new AdminEditViewModel
        {
            Id = l.Id, Title = l.Title, Description = l.Description, Price = l.Price, Currency = l.Currency,
            IsNegotiable = l.IsNegotiable, IsFree = l.IsFree, CategoryId = l.CategoryId, City = l.City,
            Region = l.Region, Condition = l.Condition, Status = l.Status, IsFeatured = l.IsFeatured,
            IsUrgent = l.IsUrgent, ModerationNote = l.ModerationNote, VideoUrl = l.VideoUrl,
            ExistingImages = l.Images.ToList(),
        };
        if (l.CarDetails is { } c)
        {
            vm.HasCar = true;
            (vm.BrandId, vm.ModelId, vm.Variant, vm.Year, vm.Mileage) = (c.BrandId, c.ModelId, c.Variant, c.Year, c.Mileage);
            (vm.Fuel, vm.Transmission, vm.Body, vm.Drive) = (c.Fuel, c.Transmission, c.Body, c.Drive);
            (vm.EngineCc, vm.PowerHp, vm.Color) = (c.EngineCc, c.PowerHp, c.Color);
            vm.Options = c.Options.Flags().ToList();
        }
        if (l.RealEstateDetails is { } r)
        {
            vm.HasRealEstate = true;
            (vm.PropertyType, vm.Transaction, vm.Rooms, vm.SurfaceM2) = (r.PropertyType, r.Transaction, r.Rooms, r.SurfaceM2);
            (vm.Floor, vm.TotalFloors, vm.YearBuilt) = (r.Floor, r.TotalFloors, r.YearBuilt);
            (vm.Heating, vm.Furnished) = (r.Heating, r.Furnished);
            vm.Amenities = r.Amenities.Flags().ToList();
        }
        await PopulateAsync(vm);
        return View(vm);
    }

    [HttpPost("listings/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminEditViewModel vm)
    {
        var l = await db.Listings
            .Include(x => x.Images)
            .Include(x => x.CarDetails)
            .Include(x => x.RealEstateDetails)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();

        l.Title = vm.Title.Trim();
        l.Description = vm.Description.Trim();
        l.Price = vm.IsFree ? null : vm.Price;
        l.Currency = vm.Currency;
        l.IsNegotiable = vm.IsNegotiable;
        l.IsFree = vm.IsFree;
        l.CategoryId = vm.CategoryId;
        l.City = vm.City.Trim();
        l.Region = vm.Region?.Trim() ?? "";
        l.Condition = vm.Condition;
        l.Status = vm.Status;
        l.IsFeatured = vm.IsFeatured;
        l.IsUrgent = vm.IsUrgent;

        if (l.CarDetails is { } c)
        {
            c.BrandId = vm.BrandId; c.ModelId = vm.ModelId; c.Variant = vm.Variant;
            c.Year = vm.Year ?? c.Year; c.Mileage = vm.Mileage ?? c.Mileage;
            c.Fuel = vm.Fuel ?? c.Fuel; c.Transmission = vm.Transmission ?? c.Transmission;
            c.Body = vm.Body ?? c.Body; c.Drive = vm.Drive ?? c.Drive;
            c.EngineCc = vm.EngineCc; c.PowerHp = vm.PowerHp; c.Color = vm.Color;
            c.Options = vm.Options.Aggregate(CarOptions.None, (a, o) => a | o);
        }
        if (l.RealEstateDetails is { } r)
        {
            r.PropertyType = vm.PropertyType ?? r.PropertyType;
            r.Transaction = vm.Transaction ?? r.Transaction;
            r.Rooms = vm.Rooms; r.SurfaceM2 = vm.SurfaceM2 ?? r.SurfaceM2;
            r.Floor = vm.Floor; r.TotalFloors = vm.TotalFloors; r.YearBuilt = vm.YearBuilt;
            r.Heating = vm.Heating ?? r.Heating; r.Furnished = vm.Furnished ?? r.Furnished;
            r.Amenities = vm.Amenities.Aggregate(PropertyAmenities.None, (a, x) => a | x);
        }

        // remove selected images
        if (vm.RemoveImageIds.Count > 0)
        {
            var toRemove = l.Images.Where(i => vm.RemoveImageIds.Contains(i.Id)).ToList();
            db.ListingImages.RemoveRange(toRemove);
        }
        // add new images from URLs (SSRF-guarded download)
        if (!string.IsNullOrWhiteSpace(vm.AddImageUrls))
        {
            var order = l.Images.Count == 0 ? 0 : l.Images.Max(i => i.SortOrder) + 1;
            foreach (var url in vm.AddImageUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (l.Images.Count >= PhotoStorage.MaxPhotos) break;
                try
                {
                    if (await fetcher.FetchImageAsync(url) is { } dl &&
                        await photos.SaveDownloadedAsync(dl.Data, dl.ContentType) is { } saved)
                        l.Images.Add(new ListingImage { Url = saved, SortOrder = order++ });
                }
                catch { /* skip bad url */ }
            }
        }

        await db.SaveChangesAsync();
        TempData["Flash"] = $"Saved: {l.Title}";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("listings/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await db.Listings.FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        db.Listings.Remove(l);
        await db.SaveChangesAsync();
        TempData["Flash"] = $"Deleted: {l.Title}";
        // back to the same filtered moderation view (never to the deleted ad's edit page)
        var referer = Request.Headers.Referer.FirstOrDefault();
        return Redirect(referer is not null && referer.Contains("/admin") && !referer.Contains("/edit") ? referer : "/admin");
    }

    // ---------- banners ----------

    [HttpGet("banners")]
    public async Task<IActionResult> Banners()
    {
        var banners = await db.Banners.AsNoTracking()
            .OrderBy(b => b.Placement).ThenBy(b => b.SortOrder).ToListAsync();
        return View(banners);
    }

    [HttpGet("banners/create")]
    public IActionResult BannerCreate() => View("BannerForm", new Banner());

    [HttpGet("banners/{id:int}/edit")]
    public async Task<IActionResult> BannerEdit(int id)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id);
        return banner is null ? NotFound() : View("BannerForm", banner);
    }

    [HttpPost("banners/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BannerSave(Banner form)
    {
        if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.ImageUrl) || string.IsNullOrWhiteSpace(form.LinkUrl))
        {
            ModelState.AddModelError("", "Title, image URL and link URL are required.");
            return View("BannerForm", form);
        }

        if (form.Id == 0)
        {
            db.Banners.Add(form);
        }
        else
        {
            var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == form.Id);
            if (banner is null) return NotFound();
            banner.Title = form.Title; banner.ImageUrl = form.ImageUrl; banner.LinkUrl = form.LinkUrl;
            banner.Placement = form.Placement; banner.IsActive = form.IsActive; banner.SortOrder = form.SortOrder;
            banner.StartsAt = form.StartsAt; banner.EndsAt = form.EndsAt;
        }
        await db.SaveChangesAsync();
        TempData["Flash"] = "Banner saved.";
        return RedirectToAction(nameof(Banners));
    }

    [HttpPost("banners/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BannerToggle(int id)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null) return NotFound();
        banner.IsActive = !banner.IsActive;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Banners));
    }

    [HttpPost("banners/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BannerDelete(int id)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null) return NotFound();
        db.Banners.Remove(banner);
        await db.SaveChangesAsync();
        TempData["Flash"] = "Banner deleted.";
        return RedirectToAction(nameof(Banners));
    }

    // ---------- settings ----------

    [HttpGet("settings")]
    public IActionResult Settings()
    {
        ViewBag.HoldAll = settings.GetBool(SettingsStore.HoldAllKey);
        ViewBag.BannedWords = settings.Get(SettingsStore.BannedWordsKey);
        ViewBag.DangerWords = settings.Get(SettingsStore.DangerWordsKey);
        return View();
    }

    [HttpPost("settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(bool holdAll, string? bannedWords, string? dangerWords)
    {
        await settings.SetAsync(SettingsStore.HoldAllKey, holdAll ? "true" : "false");
        await settings.SetAsync(SettingsStore.BannedWordsKey, bannedWords);
        await settings.SetAsync(SettingsStore.DangerWordsKey, dangerWords);
        TempData["Flash"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    /// <summary>Sends a test email to the admin — verifies the SMTP configuration live.</summary>
    [HttpPost("settings/test-email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestEmail([FromServices] IEmailSender email)
    {
        var to = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(to)) { TempData["Flash"] = "No admin email on file."; return RedirectToAction(nameof(Settings)); }
        try
        {
            await email.SendAsync(to, "AiciGăsești — test email",
                "<h2>Email works</h2><p>Your SMTP configuration is sending correctly.</p>");
            TempData["Flash"] = $"Test email sent to {to}.";
        }
        catch (Exception ex)
        {
            TempData["Flash"] = $"Email failed: {ex.Message}";
        }
        return RedirectToAction(nameof(Settings));
    }

    // ---------- users ----------

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? q)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => EF.Functions.Like(u.Email!, $"%{q}%") || EF.Functions.Like(u.DisplayName, $"%{q}%"));

        var users = await query.OrderByDescending(u => u.CreatedAt).Take(100)
            .Select(u => new AdminUserRow
            {
                Id = u.Id, Email = u.Email ?? "", DisplayName = u.DisplayName,
                City = u.City, IsBusiness = u.IsBusiness, IsAdmin = u.IsAdmin, IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt, ListingCount = u.Listings.Count,
                RatingAverage = u.RatingAverage, RatingCount = u.RatingCount,
            })
            .ToListAsync();
        ViewBag.Query = q;
        return View(users);
    }

    [HttpPost("users/{id}/ban")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(string id, bool ban)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        user.IsBanned = ban;
        // hide/restore their active listings when banning/unbanning
        var affected = await db.Listings
            .Where(l => l.UserId == id && (ban ? l.Status == ListingStatus.Active : l.Status == ListingStatus.Suspended))
            .ToListAsync();
        foreach (var l in affected) l.Status = ban ? ListingStatus.Suspended : ListingStatus.Active;
        await db.SaveChangesAsync();
        TempData["Flash"] = ban ? "User banned." : "User unbanned.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("users/{id}/admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id, bool grant)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (grant && !AdminAccess.HasAdminDomain(user.Email))
        {
            TempData["Flash"] = $"Only @{AdminAccess.RequiredDomain} addresses can be admins.";
            return RedirectToAction(nameof(Users));
        }
        user.IsAdmin = grant;
        await db.SaveChangesAsync();
        TempData["Flash"] = grant ? "Admin granted." : "Admin revoked.";
        return RedirectToAction(nameof(Users));
    }

    private async Task PopulateAsync(AdminEditViewModel vm)
    {
        vm.CategoryTree = await db.Categories.AsNoTracking()
            .Include(c => c.Children.OrderBy(ch => ch.SortOrder))
            .Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToListAsync();
        vm.Brands = await db.CarBrands.AsNoTracking().OrderByDescending(b => b.IsPopular).ThenBy(b => b.Name).ToListAsync();
    }
}

public class AdminUserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? City { get; set; }
    public bool IsBusiness { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ListingCount { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
}
