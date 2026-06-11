using System.Text.Json;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using FabClassifiedAds.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Periodically re-runs saved searches with email alerts enabled and emails
/// users about listings posted since the last check.
/// </summary>
public class SavedSearchAlertService(IServiceScopeFactory scopeFactory, ILogger<SavedSearchAlertService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // let the app finish seeding before the first pass
        await Task.Delay(TimeSpan.FromSeconds(20), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Saved-search alert pass failed");
            }
            await Task.Delay(Interval, ct);
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var search = scope.ServiceProvider.GetRequiredService<SearchService>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var searches = await db.SavedSearches
            .Include(s => s.User)
            .Where(s => s.EmailAlerts)
            .ToListAsync(ct);

        foreach (var saved in searches)
        {
            SearchFilters filters;
            try
            {
                filters = JsonSerializer.Deserialize<SearchFilters>(saved.FiltersJson) ?? new SearchFilters();
            }
            catch (JsonException)
            {
                continue;
            }
            filters.Page = 1;
            filters.PageSize = 10;
            filters.Sort = SortOption.Newest;

            var since = saved.LastCheckedAt;
            saved.LastCheckedAt = DateTime.UtcNow;

            var result = await search.SearchAsync(filters);
            var fresh = result.Items.Where(l => l.CreatedAt > since && l.UserId != saved.UserId).ToList();
            if (fresh.Count == 0) continue;

            var rows = string.Join("", fresh.Select(l =>
                $"<li><a href=\"/l/{l.Id}\">{System.Net.WebUtility.HtmlEncode(l.Title)}</a> — {System.Net.WebUtility.HtmlEncode(l.Price())} ({System.Net.WebUtility.HtmlEncode(l.City)})</li>"));
            var body = $"""
                <h2>New ads for "{System.Net.WebUtility.HtmlEncode(saved.Name)}"</h2>
                <p>{fresh.Count} new listing(s) match your saved search:</p>
                <ul>{rows}</ul>
                <p><a href="/search{saved.QueryString}">Open this search on FabAds</a></p>
                """;
            await email.SendAsync(saved.User.Email!, $"🔔 {fresh.Count} new ads: {saved.Name}", body, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
