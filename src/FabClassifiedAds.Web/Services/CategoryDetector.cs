using System.Globalization;
using System.Text;
using FabClassifiedAds.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

public record CategorySuggestion(string? Slug, int? CategoryId, int? BrandId, int? ModelId);

/// <summary>
/// Best-effort category guesser from a listing's title + description. Keyword and
/// brand based (RO + EN, diacritics-insensitive). Used to pre-select the category on
/// the cross-post form and to fill it in the create API when the caller omits it.
/// Returns an empty suggestion when nothing matches — the caller then asks the user.
/// </summary>
public class CategoryDetector(AppDbContext db)
{
    // ordered rules: (slug, any-of keywords). First match wins after the car check.
    private static readonly (string Slug, string[] Words)[] Rules =
    [
        ("iphone", ["iphone"]),
        ("samsung", ["samsung galaxy", "galaxy s", "galaxy a", "galaxy note"]),
        ("laptops", ["laptop", "macbook", "notebook", "thinkpad", "ideapad"]),
        ("gaming-pcs", ["gaming pc", "pc gaming", "rtx", "geforce", "placa video", "ryzen"]),
        ("playstation", ["playstation", "ps5", "ps4"]),
        ("xbox", ["xbox"]),
        ("televisions", ["televizor", "smart tv", "led tv", "oled", "qled"]),
        ("mobile-phones", ["telefon", "smartphone", "xiaomi", "huawei", "pixel", "oneplus"]),
        ("mountain-bikes", ["mountain bike", "mtb", "bicicleta enduro"]),
        ("bicycles", ["bicicleta", "bike", "e-bike", "ebike"]),
        ("scooters-and-mopeds", ["trotineta", "scuter", "moped"]),
        ("sofas-and-armchairs", ["canapea", "coltar", "fotoliu"]),
        ("dogs", ["catel", "caine", "pui de", "golden retriever", "labrador"]),
        ("cats", ["pisica", "pisic", "british shorthair"]),
    ];

    private static readonly string[] RentWords =
        ["inchiriez", "inchiriere", "de inchiriat", "chirie", "regim hotelier", "for rent", "rent"];
    private static readonly string[] CarWords =
        ["km", "benzina", "motorina", "diesel", "cutie", "caroserie", "autoturism", "an fabricatie",
         "rulati", "masina", "vand auto", "berlina", "hatchback", "break", "cai putere", "cp "];

    public async Task<CategorySuggestion> DetectAsync(string? title, string? description)
    {
        var text = " " + Normalize($"{title} {title} {description}") + " "; // title weighted x2
        var slugs = await db.Categories.AsNoTracking().Select(c => c.Slug).ToListAsync();
        var slugSet = slugs.ToHashSet();

        // 1) cars — a known brand token, corroborated by a car keyword (avoids false hits)
        var brands = await db.CarBrands.AsNoTracking()
            .Select(b => new { b.Id, b.Name }).ToListAsync();
        var hasCarWord = CarWords.Any(w => text.Contains(" " + w) || text.Contains(w + " "));
        foreach (var brand in brands.OrderByDescending(b => b.Name.Length))
        {
            var token = Normalize(brand.Name);
            if (token.Length < 3) continue;
            if (!text.Contains($" {token} ") && !text.Contains($" {token}-")) continue;
            if (!hasCarWord && !StartsWith(text, token)) continue;   // "BMW ..." in the title is enough

            var models = await db.CarModels.AsNoTracking()
                .Where(m => m.BrandId == brand.Id)
                .Select(m => new { m.Id, m.Name }).ToListAsync();
            int? modelId = models
                .OrderByDescending(m => m.Name.Length)
                .Where(m => text.Contains(" " + Normalize(m.Name) + " "))
                .Select(m => (int?)m.Id).FirstOrDefault();

            return await Resolve("cars", slugSet, brand.Id, modelId);
        }
        if (hasCarWord)
            return await Resolve("cars", slugSet, null, null);

        // 2) real estate — pick the sale/rent variant
        var isRent = RentWords.Any(w => text.Contains(w));
        if (ContainsAny(text, "apartament", "garsoniera", "apartamente"))
            return await Resolve(isRent ? "apartments-for-rent" : "apartments-for-sale", slugSet, null, null);
        if (ContainsAny(text, "casa", "vila", "duplex"))
            return await Resolve(isRent ? "houses-for-rent" : "houses-for-sale", slugSet, null, null);
        if (ContainsAny(text, "teren", "lot ", "parcela", "intravilan", "extravilan"))
            return await Resolve("land-and-plots", slugSet, null, null);
        if (ContainsAny(text, "spatiu comercial", "spatiu birou", "birou "))
            return await Resolve("offices", slugSet, null, null);
        if (ContainsAny(text, "garaj", "loc de parcare"))
            return await Resolve("garages-and-parking", slugSet, null, null);

        // 3) keyword rules
        foreach (var (slug, words) in Rules)
            if (words.Any(w => text.Contains(w)))
                return await Resolve(slug, slugSet, null, null);

        return new CategorySuggestion(null, null, null, null);
    }

    private async Task<CategorySuggestion> Resolve(string slug, HashSet<string> valid, int? brandId, int? modelId)
    {
        if (!valid.Contains(slug)) return new CategorySuggestion(null, null, brandId, modelId);
        var id = await db.Categories.AsNoTracking().Where(c => c.Slug == slug).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        return new CategorySuggestion(slug, id, brandId, modelId);
    }

    private static bool ContainsAny(string text, params string[] words) => words.Any(text.Contains);
    private static bool StartsWith(string paddedText, string token) => paddedText.TrimStart().StartsWith(token);

    private static string Normalize(string s)
    {
        var lower = (s ?? "").ToLowerInvariant();
        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        // collapse whitespace
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
