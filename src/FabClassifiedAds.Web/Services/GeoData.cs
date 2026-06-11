namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Offline geocoder for common cities so listings get map coordinates without
/// an external geocoding API. Unknown cities simply get no coordinates.
/// </summary>
public static class GeoData
{
    private static readonly Dictionary<string, (double Lat, double Lng)> Cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bucharest"] = (44.4268, 26.1025),
        ["Bucuresti"] = (44.4268, 26.1025),
        ["Cluj-Napoca"] = (46.7712, 23.6236),
        ["Timisoara"] = (45.7489, 21.2087),
        ["Iasi"] = (47.1585, 27.6014),
        ["Constanta"] = (44.1598, 28.6348),
        ["Brasov"] = (45.6580, 25.6012),
        ["Craiova"] = (44.3302, 23.7949),
        ["Galati"] = (45.4353, 28.0080),
        ["Oradea"] = (47.0465, 21.9189),
        ["Ploiesti"] = (44.9419, 26.0225),
        ["Sibiu"] = (45.7983, 24.1256),
        ["Arad"] = (46.1866, 21.3123),
        ["Pitesti"] = (44.8565, 24.8692),
        ["Bacau"] = (46.5670, 26.9146),
        ["Targu Mures"] = (46.5425, 24.5575),
        ["Baia Mare"] = (47.6573, 23.5681),
        ["Buzau"] = (45.1500, 26.8167),
        ["Suceava"] = (47.6635, 26.2535),
        ["Corbeanca"] = (44.5990, 26.0379),
        ["Feleacu"] = (46.7180, 23.6230),
        ["Otopeni"] = (44.5500, 26.0667),
        ["Voluntari"] = (44.4925, 26.1914),
        ["Floresti"] = (46.7448, 23.4889),
        ["Mamaia"] = (44.2550, 28.6180),
    };

    /// <summary>Returns coordinates for a known city with a small deterministic jitter so markers don't stack.</summary>
    public static (double Lat, double Lng)? Locate(string? city, int jitterSeed = 0)
    {
        if (string.IsNullOrWhiteSpace(city) || !Cities.TryGetValue(city.Trim(), out var c)) return null;
        var rnd = new Random(city.GetHashCode(StringComparison.OrdinalIgnoreCase) ^ jitterSeed);
        return (c.Lat + (rnd.NextDouble() - 0.5) * 0.05, c.Lng + (rnd.NextDouble() - 0.5) * 0.07);
    }
}
