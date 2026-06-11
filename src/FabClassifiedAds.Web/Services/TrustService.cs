using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

public enum TrustLevel { Good, Info, Warning, Danger }

public record TrustCheck(string Label, TrustLevel Level, string? Detail = null);

public class PriceVerdict
{
    public decimal Median { get; init; }
    public int SampleSize { get; init; }
    /// <summary>Negative = below market median.</summary>
    public double DiffPercent { get; init; }
    public TrustLevel Level { get; init; }
}

public class TrustReport
{
    public int Score { get; set; } = 100;
    public List<TrustCheck> Checks { get; } = [];
    public PriceVerdict? Price { get; set; }

    public string CssClass => Score >= 75 ? "trust-good" : Score >= 40 ? "trust-warn" : "trust-bad";

    public void Add(string label, TrustLevel level, int penalty = 0, string? detail = null)
    {
        Checks.Add(new TrustCheck(label, level, detail));
        Score = Math.Clamp(Score - penalty, 5, 100);
    }
}

/// <summary>
/// Rule-based + statistical trust engine: scores listings and sellers, verifies
/// prices against the live market median, and flags risky chat messages in real
/// time. Deliberately transparent heuristics — an ML model can replace individual
/// signals later without changing the UI contract.
/// </summary>
public class TrustService(AppDbContext db)
{
    // ---------- real-time chat analysis ----------

    private static readonly (string Flag, Regex Pattern)[] MessagePatterns =
    [
        ("AdvancePayment", new Regex(@"\b(avans\w*|plata\s+in\s+avans|plata\s+inainte|transfer\s+(bancar\s+)?inainte|achita\s+inainte|trimit\s+(coletul\s+)?dupa\s+plata|deposit|pay\s+in\s+advance|payment\s+upfront)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("SuspiciousLink", new Regex(@"https?://|www\.|bit\.ly|tinyurl|t\.me/", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("OffPlatform", new Regex(@"\b(doar\s+whatsapp|scrie(ti)?\s+pe\s+whatsapp|whats?app|telegram|viber)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("UntraceablePayment", new Regex(@"\b(western\s+union|moneygram|gift\s*card|crypto|bitcoin|usdt|paysafe)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
    ];

    /// <summary>Returns flag keys for a chat message ("" when clean). Diacritics-insensitive.</summary>
    public static string AnalyzeMessage(string body)
    {
        var normalized = RemoveDiacritics(body);
        var flags = MessagePatterns.Where(p => p.Pattern.IsMatch(normalized)).Select(p => p.Flag);
        return string.Join(",", flags);
    }

    // ---------- listing analysis ----------

    private static readonly Regex ScamLanguage = new(
        @"\b(plata\s+in\s+avans|avans\s+(prin|de)|transfer\s+bancar\s+inainte|western\s+union|moneygram|gift\s*card|" +
        @"doar\s+whatsapp|contact\s+doar\s+pe\s+whatsapp|trimit\s+(gratuit\s+)?prin\s+curier\s+dupa\s+plata|" +
        @"urgent[!\s]|plecat\s+din\s+tara|sunt\s+in\s+strainatate|garantez\s+returnarea|anydesk|teamviewer|crypto|bitcoin)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LinkPattern = new(@"https?://|www\.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b07\d{8}\b|\b\+?40\d{9}\b", RegexOptions.Compiled);

    public async Task<TrustReport> AnalyzeListingAsync(Listing l)
    {
        var report = new TrustReport();
        var text = RemoveDiacritics($"{l.Title} {l.Description}");

        // 1. language
        var scamHits = ScamLanguage.Matches(text).Count;
        if (scamHits > 0)
            report.Add("Risky language detected", TrustLevel.Danger, Math.Min(25 * scamHits, 50), $"×{scamHits}");
        if (LinkPattern.IsMatch(l.Description))
            report.Add("Link in description", TrustLevel.Warning, 15);
        if (PhonePattern.IsMatch(l.Title))
            report.Add("Phone number in title", TrustLevel.Warning, 5);

        var letters = l.Title.Count(char.IsLetter);
        if (letters > 10 && l.Title.Count(char.IsUpper) > letters * 0.6)
            report.Add("ALL CAPS title", TrustLevel.Warning, 5);

        if (l.Description.Length < 80)
            report.Add("Very short description", TrustLevel.Warning, 10);
        else
            report.Add("Detailed description", TrustLevel.Good);

        // 2. copy/paste duplicates from other accounts
        var fingerprint = Fingerprint(l.Description);
        if (fingerprint.Length >= 40)
        {
            var candidates = await db.Listings.AsNoTracking()
                .Where(x => x.Id != l.Id && x.UserId != l.UserId)
                .Select(x => x.Description).ToListAsync();
            if (candidates.Any(d => Fingerprint(d) == fingerprint))
                report.Add("Duplicate description (copy/paste)", TrustLevel.Danger, 30);
        }

        // 3. media
        if (!string.IsNullOrEmpty(l.VideoUrl))
            report.Add("Has video", TrustLevel.Good);
        if (l.Images.Any(i => i.Url.StartsWith("/uploads/")))
            report.Add("Real photos uploaded", TrustLevel.Good);
        else
            report.Add("Generic images only", TrustLevel.Info, 5);

        // 4. price vs market
        report.Price = await CheckPriceAsync(l);
        switch (report.Price)
        {
            case { Level: TrustLevel.Danger }:
                report.Add("Price far below market", TrustLevel.Danger, 35, $"{report.Price.DiffPercent:+0;-0}%");
                break;
            case { Level: TrustLevel.Warning }:
                report.Add("Price below market", TrustLevel.Warning, 15, $"{report.Price.DiffPercent:+0;-0}%");
                break;
            case { Level: TrustLevel.Good }:
                report.Add("Realistic price for this market", TrustLevel.Good, 0, $"{report.Price.DiffPercent:+0;-0}%");
                break;
        }

        return report;
    }

    /// <summary>
    /// Compares the asking price against the median of comparable listings:
    /// same car brand (±2 years when enough samples), price/m² for the same
    /// property type and transaction, or the category median otherwise.
    /// </summary>
    public async Task<PriceVerdict?> CheckPriceAsync(Listing l)
    {
        if (l.Price is null or <= 0) return null;

        List<decimal> samples;
        decimal subject = l.Price.Value;

        if (l.CarDetails is { BrandId: not null } car)
        {
            samples = await db.CarDetails.AsNoTracking()
                .Where(c => c.BrandId == car.BrandId && c.ListingId != l.Id &&
                            Math.Abs(c.Year - car.Year) <= 2 &&
                            c.Listing.Price != null && c.Listing.Status == ListingStatus.Active)
                .Select(c => c.Listing.Price!.Value).ToListAsync();
            if (samples.Count < 2)
                samples = await db.CarDetails.AsNoTracking()
                    .Where(c => c.BrandId == car.BrandId && c.ListingId != l.Id &&
                                c.Listing.Price != null && c.Listing.Status == ListingStatus.Active)
                    .Select(c => c.Listing.Price!.Value).ToListAsync();
            if (samples.Count < 2)
                samples = await db.Listings.AsNoTracking()
                    .Where(x => x.CategoryId == l.CategoryId && x.Id != l.Id &&
                                x.Price != null && x.Status == ListingStatus.Active)
                    .Select(x => x.Price!.Value).ToListAsync();
        }
        else if (l.RealEstateDetails is { SurfaceM2: > 0 } re)
        {
            // compare per square metre within the same property type and transaction
            var perM2 = await db.RealEstateDetails.AsNoTracking()
                .Where(r => r.PropertyType == re.PropertyType && r.Transaction == re.Transaction &&
                            r.ListingId != l.Id && r.SurfaceM2 > 0 &&
                            r.Listing.Price != null && r.Listing.Status == ListingStatus.Active)
                .Select(r => new { r.Listing.Price, r.SurfaceM2 }).ToListAsync();
            samples = perM2.Select(x => x.Price!.Value / (decimal)x.SurfaceM2).ToList();
            subject = l.Price.Value / (decimal)re.SurfaceM2;
        }
        else
        {
            samples = await db.Listings.AsNoTracking()
                .Where(x => x.CategoryId == l.CategoryId && x.Id != l.Id &&
                            x.Price != null && x.Status == ListingStatus.Active)
                .Select(x => x.Price!.Value).ToListAsync();
        }

        if (samples.Count < 2) return null;

        samples.Sort();
        var median = samples[samples.Count / 2];
        if (median <= 0) return null;

        var diff = (double)((subject - median) / median * 100);
        return new PriceVerdict
        {
            Median = Math.Round(median, 0),
            SampleSize = samples.Count,
            DiffPercent = Math.Round(diff, 0),
            Level = diff <= -50 ? TrustLevel.Danger
                  : diff <= -30 ? TrustLevel.Warning
                  : Math.Abs(diff) <= 25 ? TrustLevel.Good
                  : TrustLevel.Info,
        };
    }

    // ---------- seller analysis ----------

    public async Task<TrustReport> AnalyzeSellerAsync(string userId)
    {
        var report = new TrustReport();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);

        var ageDays = (DateTime.UtcNow - user.CreatedAt).TotalDays;
        if (ageDays < 7) report.Add("New account", TrustLevel.Warning, 25, $"{(int)ageDays}d");
        else if (ageDays < 30) report.Add("Recent account", TrustLevel.Info, 10);
        else report.Add("Established account", TrustLevel.Good, 0, user.CreatedAt.ToString("MMM yyyy", CultureInfo.InvariantCulture));

        if (user.EmailConfirmed) report.Add("Email confirmed", TrustLevel.Good);
        else report.Add("Email not confirmed", TrustLevel.Warning, 10);

        if (user.RatingCount >= 5 && user.RatingAverage < 3.5)
            report.Add("Low ratings", TrustLevel.Danger, 20, $"★ {user.RatingAverage:0.0}");
        else if (user.RatingCount > 0)
            report.Add("Good ratings", TrustLevel.Good, 0, $"★ {user.RatingAverage:0.0} ({user.RatingCount})");
        else
            report.Add("No ratings yet", TrustLevel.Info, 5);

        // risky behaviour in chat
        var sent = await db.Messages.AsNoTracking().Where(m => m.SenderId == userId).ToListAsync();
        if (sent.Count >= 3)
        {
            var flaggedRatio = sent.Count(m => !string.IsNullOrEmpty(m.RiskFlags)) / (double)sent.Count;
            if (flaggedRatio > 0.3) report.Add("Risky messages sent", TrustLevel.Danger, 30);
            else if (flaggedRatio > 0) report.Add("Some risky messages sent", TrustLevel.Warning, 10);
        }

        // response speed: time from a buyer's first message to the seller's first reply
        var conversations = await db.Conversations.AsNoTracking()
            .Where(c => c.SellerId == userId)
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .Take(20).ToListAsync();
        var responseTimes = conversations
            .Select(c =>
            {
                var ask = c.Messages.FirstOrDefault(m => m.SenderId != userId);
                var reply = ask == null ? null : c.Messages.FirstOrDefault(m => m.SenderId == userId && m.SentAt > ask.SentAt);
                return ask != null && reply != null ? (reply.SentAt - ask.SentAt).TotalMinutes : (double?)null;
            })
            .Where(t => t.HasValue).Select(t => t!.Value).ToList();
        if (responseTimes.Count >= 2 && responseTimes.Average() < 60)
            report.Add("Responds quickly", TrustLevel.Good);

        if (user.IsBusiness) report.Add("Business account", TrustLevel.Good);

        return report;
    }

    public static string ScoreLabel(int score) =>
        score >= 75 ? "Trusted seller" : score >= 40 ? "Caution advised" : "Avoid this seller";

    public static string ListingScoreLabel(int score) =>
        score >= 75 ? "Trusted listing" : score >= 40 ? "Caution advised" : "High risk";

    // ---------- helpers ----------

    private static string Fingerprint(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in RemoveDiacritics(text).ToLowerInvariant())
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        var s = sb.ToString();
        return s.Length > 200 ? s[..200] : s;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
