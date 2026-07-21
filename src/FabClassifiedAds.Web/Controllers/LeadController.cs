using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Public growth funnel: a signed-out visitor pastes a link to their OLX/Autovit ad,
/// we auto-fill it (schema.org / OpenGraph), then they create a lightweight account
/// (name, email, password, phone) and the ad is published in one step.
/// </summary>
public class LeadController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ListingUrlImporter urlImporter,
    CategoryDetector detector,
    SafeHttpFetcher fetcher,
    PhotoStorage photos,
    ContentModerationService moderation,
    SettingsStore settings) : Controller
{
    private static readonly string[] AvatarColors = ["#6d5dfc", "#fc5d8d", "#28c76f", "#ff9f43", "#00cfe8", "#ea5455"];

    [HttpGet("/start")]
    public async Task<IActionResult> Index()
    {
        // already signed in? send them straight to the normal post form
        if (User.Identity?.IsAuthenticated == true) return Redirect("/post");
        return View(await PopulateAsync(new QuickStartViewModel()));
    }

    /// <summary>Anonymous preview: fetch the pasted link and return pre-fill data + a suggested category.</summary>
    [HttpPost("/start/preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm] string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !fetcher.IsFetchableUrl(url))
            return Json(new { ok = false, error = "Please paste a full http(s) link." });
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var prefill = await urlImporter.ImportAsync(url, cts.Token);
            if (prefill is null || string.IsNullOrWhiteSpace(prefill.Title))
                return Json(new { ok = false, error = "Couldn't read that page. Fill the details in manually." });

            var suggestion = await detector.DetectAsync(prefill.Title, prefill.Description);
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
                    sourceUrl = url,
                }
            });
        }
        catch
        {
            return Json(new { ok = false, error = "Couldn't reach that link. Fill the details in manually." });
        }
    }

    /// <summary>Create the account and publish the pasted ad in one step.</summary>
    [HttpPost("/start/register")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Register(QuickStartViewModel vm)
    {
        if (!vm.AcceptTerms)
            ModelState.AddModelError(nameof(vm.AcceptTerms), "You must accept the Terms and Privacy Policy.");
        var category = await db.Categories.FindAsync(vm.CategoryId);
        if (category is null) ModelState.AddModelError(nameof(vm.CategoryId), "Please pick a category.");

        if (!ModelState.IsValid) return View("Index", await PopulateAsync(vm));

        // create the account
        var user = new ApplicationUser
        {
            UserName = vm.Email, Email = vm.Email, DisplayName = vm.DisplayName.Trim(),
            PhoneNumber = vm.Phone.Trim(),
            AvatarColor = AvatarColors[Math.Abs(vm.Email.GetHashCode()) % AvatarColors.Length],
        };
        var created = await userManager.CreateAsync(user, vm.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors) ModelState.AddModelError("", e.Description);
            return View("Index", await PopulateAsync(vm));
        }
        await signInManager.SignInAsync(user, isPersistent: true);

        // moderation → publish or hold
        var verdict = moderation.Analyze(vm.Title, vm.Description);
        var holdAll = settings.GetBool(SettingsStore.HoldAllKey);
        var status = verdict.NeedsReview || holdAll ? ListingStatus.PendingReview : ListingStatus.Active;

        var listing = new Listing
        {
            Title = vm.Title.Trim(),
            Description = vm.Description.Trim(),
            Price = vm.Price,
            Currency = string.IsNullOrWhiteSpace(vm.Currency) ? "EUR" : vm.Currency,
            CategoryId = category!.Id,
            City = vm.City.Trim(),
            Region = vm.Region?.Trim() ?? "",
            ContactPhone = vm.Phone.Trim(),
            UserId = user.Id,
            Status = status,
            ModerationNote = verdict.Note,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };

        var order = 0;
        foreach (var remote in (vm.ImageUrls ?? []).Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            if (order >= PhotoStorage.MaxPhotos) break;
            try
            {
                if (await fetcher.FetchImageAsync(remote) is { } dl &&
                    await photos.SaveDownloadedAsync(dl.Data, dl.ContentType) is { } saved)
                    listing.Images.Add(new ListingImage { Url = saved, SortOrder = order++ });
            }
            catch { /* skip */ }
        }
        if (listing.Images.Count == 0)
            listing.Images.Add(new ListingImage { Url = $"/media/ph/{CategorySeeder.Slugify(vm.Title)}-0.svg" });

        if (GeoData.Locate(listing.City, Environment.TickCount) is { } coords)
        {
            listing.Latitude = coords.Lat;
            listing.Longitude = coords.Lng;
        }

        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        TempData["Flash"] = status == ListingStatus.Active
            ? "Welcome! Your ad is live. 🎉"
            : "Welcome! Your ad was submitted and is awaiting review. ⏳";
        return Redirect($"/l/{listing.Id}");
    }

    private async Task<QuickStartViewModel> PopulateAsync(QuickStartViewModel vm)
    {
        vm.CategoryTree = await db.Categories.AsNoTracking()
            .Include(c => c.Children.OrderBy(ch => ch.SortOrder))
            .Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToListAsync();
        return vm;
    }
}
