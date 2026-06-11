using System.Globalization;
using System.Text.RegularExpressions;
using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Helpers;

public static partial class Display
{
    [GeneratedRegex("(?<!^)([A-Z0-9])")]
    private static partial Regex CamelBoundary();

    /// <summary>"PlugInHybrid" -> "Plug in hybrid"</summary>
    public static string Humanize(this Enum value)
    {
        var spaced = CamelBoundary().Replace(value.ToString(), " $1");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }

    public static string Price(this Listing l)
    {
        if (l.IsFree) return "Free";
        if (l.Price is null) return "Ask for price";
        var amount = l.Price.Value.ToString("#,0.##", CultureInfo.InvariantCulture).Replace(",", " ");
        var symbol = l.Currency switch { "EUR" => "€", "USD" => "$", "RON" => "lei", _ => l.Currency };
        var rent = l.RealEstateDetails?.Transaction is TransactionType.Rent or TransactionType.RentShortTerm ? "/month" : "";
        return $"{amount} {symbol}{rent}";
    }

    public static string TimeAgo(this DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
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
        if (l.CarDetails is { } c)
        {
            var parts = new List<string> { c.Year.ToString(), $"{c.Mileage.Number()} km", c.Fuel.Humanize(), c.Transmission.Humanize() };
            if (c.PowerHp.HasValue) parts.Add($"{c.PowerHp} hp");
            return string.Join(" • ", parts);
        }
        if (l.RealEstateDetails is { } r)
        {
            var parts = new List<string>();
            if (r.Rooms.HasValue) parts.Add($"{r.Rooms} rooms");
            parts.Add($"{r.SurfaceM2:0.#} m²");
            if (r.Floor.HasValue) parts.Add(r.Floor == 0 ? "Ground floor" : $"Floor {r.Floor}");
            if (r.YearBuilt.HasValue) parts.Add($"Built {r.YearBuilt}");
            return string.Join(" • ", parts);
        }
        return l.Condition.Humanize();
    }
}
