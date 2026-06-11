using System.Security.Claims;
using System.Text.Json;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

[Authorize]
public class SavedSearchesController(AppDbContext db) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/saved-searches")]
    public async Task<IActionResult> Index() =>
        View(await db.SavedSearches.AsNoTracking()
            .Where(s => s.UserId == UserId)
            .OrderByDescending(s => s.CreatedAt).ToListAsync());

    /// <summary>
    /// The save form posts to this action with the search's query string appended,
    /// so SearchFilters binds from the query and the name from the form body.
    /// </summary>
    [HttpPost("/saved-searches/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SearchFilters filters, string? name)
    {
        filters.Page = 1;
        var queryString = Request.QueryString.Value ?? "";

        if (string.IsNullOrWhiteSpace(name))
            name = BuildDefaultName(filters);

        var alreadySaved = await db.SavedSearches
            .AnyAsync(s => s.UserId == UserId && s.QueryString == queryString);
        if (!alreadySaved)
        {
            db.SavedSearches.Add(new SavedSearch
            {
                UserId = UserId,
                Name = name.Trim(),
                QueryString = queryString,
                FiltersJson = JsonSerializer.Serialize(filters),
            });
            await db.SaveChangesAsync();
            TempData["Flash"] = "Search saved — we'll email you when new matching ads appear. 🔔";
        }
        else
        {
            TempData["Flash"] = "You already saved this exact search.";
        }
        return Redirect($"/search{queryString}");
    }

    [HttpPost("/saved-searches/{id:int}/toggle-alerts")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAlerts(int id)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id && s.UserId == UserId);
        if (saved is null) return NotFound();
        saved.EmailAlerts = !saved.EmailAlerts;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/saved-searches/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var saved = await db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id && s.UserId == UserId);
        if (saved is null) return NotFound();
        db.SavedSearches.Remove(saved);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static string BuildDefaultName(SearchFilters f)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Q)) parts.Add($"\"{f.Q}\"");
        if (!string.IsNullOrWhiteSpace(f.Category)) parts.Add(f.Category.Replace('-', ' '));
        if (f.PriceMax.HasValue) parts.Add($"under {f.PriceMax:#,0} €");
        if (f.YearMin.HasValue) parts.Add($"{f.YearMin}+");
        if (!string.IsNullOrWhiteSpace(f.City)) parts.Add($"in {f.City}");
        return parts.Count > 0 ? string.Join(" · ", parts) : "All ads";
    }
}
