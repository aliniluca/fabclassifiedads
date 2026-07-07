using FabClassifiedAds.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>Public banner click tracking → redirect to the advertiser link.</summary>
public class BannersController(AppDbContext db) : Controller
{
    [HttpGet("/b/{id:int}/click")]
    public async Task<IActionResult> Click(int id)
    {
        var banner = await db.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (banner is null) return NotFound();

        await db.Database.ExecuteSqlRawAsync("UPDATE \"Banners\" SET \"Clicks\" = \"Clicks\" + 1 WHERE \"Id\" = {0}", id);

        if (Uri.TryCreate(banner.LinkUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return Redirect(banner.LinkUrl);
        return Redirect("/");
    }
}
