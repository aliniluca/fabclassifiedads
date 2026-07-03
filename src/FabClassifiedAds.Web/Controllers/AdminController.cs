using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

[Authorize(Policy = AdminAccess.PolicyName)]
[Route("admin")]
public class AdminController(AppDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string tab = "pending")
    {
        ViewBag.Tab = tab;
        ViewBag.Counts = new
        {
            Pending = await db.Listings.CountAsync(l => l.Status == ListingStatus.PendingReview),
            Active = await db.Listings.CountAsync(l => l.Status == ListingStatus.Active),
            Rejected = await db.Listings.CountAsync(l => l.Status == ListingStatus.Rejected),
            Total = await db.Listings.CountAsync(),
            Users = await db.Users.CountAsync(),
        };

        var status = tab switch
        {
            "rejected" => ListingStatus.Rejected,
            "active" => ListingStatus.Active,
            _ => ListingStatus.PendingReview,
        };

        var listings = await db.Listings.AsNoTracking()
            .Where(l => l.Status == status)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync();

        return View(listings);
    }

    [HttpPost("listings/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();
        listing.Status = ListingStatus.Active;
        listing.BumpedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Flash"] = $"Approved: {listing.Title}";
        return RedirectToAction(nameof(Index));
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
        return RedirectToAction(nameof(Index));
    }
}
