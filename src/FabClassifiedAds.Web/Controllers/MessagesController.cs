using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

[Authorize]
public class MessagesController(AppDbContext db) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/messages")]
    public async Task<IActionResult> Index(int? c)
    {
        var conversations = await db.Conversations.AsNoTracking()
            .Where(x => x.BuyerId == UserId || x.SellerId == UserId)
            .Include(x => x.Listing).ThenInclude(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(x => x.Buyer).Include(x => x.Seller)
            .Include(x => x.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(x => x.LastMessageAt).ToListAsync();

        Conversation? active = null;
        if (c.HasValue)
        {
            active = await db.Conversations
                .Include(x => x.Listing)
                .Include(x => x.Buyer).Include(x => x.Seller)
                .Include(x => x.Messages.OrderBy(m => m.SentAt)).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(x => x.Id == c && (x.BuyerId == UserId || x.SellerId == UserId));
            if (active != null)
            {
                foreach (var m in active.Messages.Where(m => !m.IsRead && m.SenderId != UserId))
                    m.IsRead = true;
                await db.SaveChangesAsync();
            }
        }

        ViewBag.Active = active;
        return View(conversations);
    }

    [HttpPost("/messages/start/{listingId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int listingId, string body)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId);
        if (listing is null) return NotFound();
        if (listing.UserId == UserId)
        {
            TempData["Flash"] = "You can't message yourself about your own ad.";
            return RedirectToAction("Details", "Listings", new { id = listingId });
        }

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(x => x.ListingId == listingId && x.BuyerId == UserId);
        if (conversation is null)
        {
            conversation = new Conversation { ListingId = listingId, BuyerId = UserId, SellerId = listing.UserId };
            db.Conversations.Add(conversation);
        }
        if (!string.IsNullOrWhiteSpace(body))
        {
            conversation.Messages.Add(new Message { SenderId = UserId, Body = body.Trim() });
            conversation.LastMessageAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { c = conversation.Id });
    }

    [HttpPost("/messages/{conversationId:int}/send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int conversationId, string body)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(x => x.Id == conversationId && (x.BuyerId == UserId || x.SellerId == UserId));
        if (conversation is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(body))
        {
            db.Messages.Add(new Message { ConversationId = conversationId, SenderId = UserId, Body = body.Trim() });
            conversation.LastMessageAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { c = conversationId });
    }
}
