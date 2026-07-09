using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Marks active listings as <see cref="ListingStatus.Expired"/> once their
/// <c>ExpiresAt</c> has passed. Owners then re-list them manually ("Republică").
/// Runs hourly.
/// </summary>
public class ListingMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<ListingMaintenanceService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct);   // let startup finish
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var expired = await db.Listings
                    .Where(l => l.Status == ListingStatus.Active && l.ExpiresAt != null && l.ExpiresAt < now)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, ListingStatus.Expired), ct);
                if (expired > 0)
                    logger.LogInformation("Expired {Count} listing(s) past their 30-day window", expired);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Listing expiry pass failed");
            }
            await Task.Delay(Interval, ct);
        }
    }
}
