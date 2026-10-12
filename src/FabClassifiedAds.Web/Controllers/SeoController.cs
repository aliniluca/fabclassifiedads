using System.Text;
using System.Xml;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Helpers;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>robots.txt, sitemap.xml and a health check.</summary>
public class SeoController(AppDbContext db) : Controller
{
    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var body = $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /account
            Disallow: /messages
            Disallow: /api/
            Sitemap: {host}/sitemap.xml
            """;
        return Content(body, "text/plain");
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var categories = await db.Categories.AsNoTracking().Select(c => c.Slug).ToListAsync();
        var listings = await db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .OrderByDescending(l => l.BumpedAt)
            .Take(5000)
            .Select(l => new { l.Id, l.Title, l.BumpedAt, CategoryName = l.Category.Name })
            .ToListAsync();

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), Async = true };
        await using (var w = XmlWriter.Create(sb, settings))
        {
            await w.WriteStartDocumentAsync();
            w.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            void Url(string loc, DateTime? lastMod = null)
            {
                w.WriteStartElement("url");
                w.WriteElementString("loc", loc);
                if (lastMod is { } d) w.WriteElementString("lastmod", d.ToString("yyyy-MM-dd"));
                w.WriteEndElement();
            }

            Url(host + "/");
            foreach (var slug in categories) Url($"{host}/c/{slug}");
            foreach (var l in listings)
                Url(host + Display.ListingPath(l.Id, l.Title, l.CategoryName), l.BumpedAt);

            w.WriteEndElement();
            await w.WriteEndDocumentAsync();
        }
        return Content(sb.ToString(), "application/xml");
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.UtcNow });
}
