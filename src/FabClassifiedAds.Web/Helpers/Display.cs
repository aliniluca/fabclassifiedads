using System.Globalization;
using System.Text.RegularExpressions;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;

namespace FabClassifiedAds.Web.Helpers;

public static partial class Display
{
    // Insert a space before each capital, and before a digit run ("Camera360" -> "Camera 360")
    [GeneratedRegex("(?<!^)((?<![0-9])[0-9]|[A-Z])")]
    private static partial Regex CamelBoundary();

    /// <summary>"PlugInHybrid" -> "Plug in hybrid"</summary>
    public static string Humanize(this Enum value)
    {
        var spaced = CamelBoundary().Replace(value.ToString(), " $1");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }

    public static string Price(this Listing l)
    {
        if (l.IsFree) return Translator.IsRo ? "Gratuit" : "Free";
        if (l.Price is null) return Translator.IsRo ? "Preț la cerere" : "Ask for price";
        var amount = l.Price.Value.ToString("#,0.##", CultureInfo.InvariantCulture).Replace(",", " ");
        var symbol = l.Currency switch { "EUR" => "€", "USD" => "$", "RON" => "lei", _ => l.Currency };
        var isRent = l.RealEstateDetails?.Transaction is TransactionType.Rent or TransactionType.RentShortTerm;
        var rent = isRent ? (Translator.IsRo ? "/lună" : "/month") : "";
        return $"{amount} {symbol}{rent}";
    }

    public static string TimeAgo(this DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (Translator.IsRo)
        {
            return span switch
            {
                { TotalMinutes: < 1 } => "chiar acum",
                { TotalMinutes: < 60 } => $"acum {(int)span.TotalMinutes} min",
                { TotalHours: < 24 } => $"acum {(int)span.TotalHours}h",
                { TotalDays: < 7 } => $"acum {(int)span.TotalDays} zile",
                { TotalDays: < 30 } => $"acum {(int)(span.TotalDays / 7)} săpt.",
                _ => utc.ToString("d MMM yyyy", new CultureInfo("ro")),
            };
        }
        return span switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes} min ago",
            { TotalHours: < 24 } => $"{(int)span.TotalHours}h ago",
            { TotalDays: < 7 } => $"{(int)span.TotalDays}d ago",
            { TotalDays: < 30 } => $"{(int)(span.TotalDays / 7)}w ago",
            _ => utc.ToString("d MMM yyyy"),
        };
    }

    public static string Number(this int n) => n.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");

    public static IEnumerable<TEnum> Flags<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Where(o => Convert.ToInt64(o) != 0 && value.HasFlag(o));

    public static string ListingSummary(this Listing l)
    {
        var ro = Translator.IsRo;
        if (l.CarDetails is { } c)
        {
            var parts = new List<string> { c.Year.ToString(), $"{c.Mileage.Number()} km", TranslateEnum(c.Fuel), TranslateEnum(c.Transmission) };
            if (c.PowerHp.HasValue) parts.Add(ro ? $"{c.PowerHp} CP" : $"{c.PowerHp} hp");
            return string.Join(" • ", parts);
        }
        if (l.RealEstateDetails is { } r)
        {
            var parts = new List<string>();
            if (r.Rooms.HasValue) parts.Add(ro ? $"{r.Rooms} camere" : $"{r.Rooms} rooms");
            parts.Add($"{r.SurfaceM2:0.#} m²");
            if (r.Floor.HasValue) parts.Add(r.Floor == 0 ? (ro ? "Parter" : "Ground floor") : (ro ? $"Etaj {r.Floor}" : $"Floor {r.Floor}"));
            if (r.YearBuilt.HasValue) parts.Add(ro ? $"Construit {r.YearBuilt}" : $"Built {r.YearBuilt}");
            return string.Join(" • ", parts);
        }
        return TranslateEnum(l.Condition);
    }

    private static readonly Translator T = new();

    /// <summary>Humanizes an enum value and runs it through the translator.</summary>
    public static string TranslateEnum(this Enum value) => T[value.Humanize()];
}
