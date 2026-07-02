using System.Text.Json;
using AngleSharp.Html.Parser;

namespace FabClassifiedAds.Web.Services;

/// <summary>What we managed to extract from a page, used to pre-fill the post form.</summary>
public class ListingPrefill
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public List<string> Images { get; set; } = [];
    public string SourceUrl { get; set; } = "";
}

/// <summary>
/// Reads a listing page the user owns and extracts the reusable facts. It reads
/// portable, publisher-provided metadata only — schema.org JSON-LD and OpenGraph
/// tags — so it works across marketplaces without brittle site-specific selectors.
/// </summary>
public class ListingUrlImporter(SafeHttpFetcher fetcher)
{
    public async Task<ListingPrefill?> ImportAsync(string url, CancellationToken ct = default)
    {
        if (!fetcher.IsFetchableUrl(url)) return null;
        var html = await fetcher.FetchHtmlAsync(url, ct);
        return string.IsNullOrEmpty(html) ? null : Parse(html, url);
    }

    /// <summary>Pure parse step (no network) — kept separate so it is easily testable.</summary>
    public ListingPrefill Parse(string html, string baseUrl)
    {
        var doc = new HtmlParser().ParseDocument(html);
        var result = new ListingPrefill { SourceUrl = baseUrl };
        var baseUri = Uri.TryCreate(baseUrl, UriKind.Absolute, out var b) ? b : null;

        // ---- 1. schema.org JSON-LD (richest, most portable) ----
        foreach (var node in doc.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var raw = node.TextContent?.Trim();
            if (string.IsNullOrEmpty(raw)) continue;
            try
            {
                using var json = JsonDocument.Parse(raw);
                foreach (var obj in Flatten(json.RootElement))
                    ApplyJsonLd(obj, result);
            }
            catch (JsonException) { /* skip malformed blocks */ }
        }

        // ---- 2. OpenGraph / meta fallbacks (only fill what's still missing) ----
        result.Title ??= Meta(doc, "og:title") ?? doc.QuerySelector("title")?.TextContent?.Trim();
        result.Description ??= Meta(doc, "og:description")
            ?? doc.QuerySelector("meta[name='description']")?.GetAttribute("content")?.Trim();
        result.City ??= Meta(doc, "og:locality") ?? Meta(doc, "product:locality");

        if (result.Price is null)
        {
            var priceStr = Meta(doc, "product:price:amount") ?? Meta(doc, "og:price:amount");
            if (TryParsePrice(priceStr, out var p)) result.Price = p;
            result.Currency ??= Meta(doc, "product:price:currency") ?? Meta(doc, "og:price:currency");
        }

        foreach (var m in doc.QuerySelectorAll("meta[property='og:image'], meta[property='og:image:url']"))
            AddImage(result, m.GetAttribute("content"), baseUri);

        // ---- 3. tidy up ----
        result.Title = Trim(result.Title, 120);
        result.Description = Trim(result.Description, 8000);
        result.Images = result.Images.Distinct().Take(10).ToList();
        if (string.IsNullOrWhiteSpace(result.Currency)) result.Currency = "EUR";
        return result;
    }

    /// <summary>Expand @graph and arrays into a flat list of candidate JSON objects.</summary>
    private static IEnumerable<JsonElement> Flatten(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
                foreach (var x in Flatten(item)) yield return x;
        }
        else if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("@graph", out var graph))
                foreach (var x in Flatten(graph)) yield return x;
            yield return el;
        }
    }

    private static void ApplyJsonLd(JsonElement obj, ListingPrefill r)
    {
        if (obj.ValueKind != JsonValueKind.Object) return;

        if (r.Title is null && obj.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            r.Title = name.GetString();
        if (r.Description is null && obj.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
            r.Description = desc.GetString();

        if (obj.TryGetProperty("image", out var image))
            AddJsonImages(image, r);

        if (obj.TryGetProperty("offers", out var offers))
            foreach (var offer in Flatten(offers))
            {
                if (r.Price is null && offer.TryGetProperty("price", out var price))
                {
                    if (price.ValueKind == JsonValueKind.Number) r.Price = price.GetDecimal();
                    else if (price.ValueKind == JsonValueKind.String && TryParsePrice(price.GetString(), out var p)) r.Price = p;
                }
                if (r.Currency is null && offer.TryGetProperty("priceCurrency", out var cur) && cur.ValueKind == JsonValueKind.String)
                    r.Currency = cur.GetString();
            }

        if (obj.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.Object)
        {
            if (r.City is null && addr.TryGetProperty("addressLocality", out var loc) && loc.ValueKind == JsonValueKind.String)
                r.City = loc.GetString();
            if (r.Region is null && addr.TryGetProperty("addressRegion", out var reg) && reg.ValueKind == JsonValueKind.String)
                r.Region = reg.GetString();
        }
    }

    private static void AddJsonImages(JsonElement image, ListingPrefill r)
    {
        switch (image.ValueKind)
        {
            case JsonValueKind.String:
                AddImage(r, image.GetString(), null);
                break;
            case JsonValueKind.Array:
                foreach (var i in image.EnumerateArray()) AddJsonImages(i, r);
                break;
            case JsonValueKind.Object:
                if (image.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                    AddImage(r, u.GetString(), null);
                break;
        }
    }

    private static void AddImage(ListingPrefill r, string? url, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(baseUri, url, out var abs)) return;
        if (abs.Scheme is "http" or "https" && r.Images.Count < 20)
            r.Images.Add(abs.ToString());
    }

    private static string? Meta(AngleSharp.Dom.IDocument doc, string property) =>
        doc.QuerySelector($"meta[property='{property}']")?.GetAttribute("content")?.Trim() is { Length: > 0 } v ? v : null;

    private static bool TryParsePrice(string? s, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var digits = new string(s.Where(c => char.IsDigit(c) || c is '.' or ',').ToArray())
            .Replace(".", "").Replace(",", ".");
        return decimal.TryParse(digits, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out price) && price > 0;
    }

    private static string? Trim(string? s, int max) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Length > max ? s[..max] : s).Trim();
}
