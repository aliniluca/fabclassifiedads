using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FabClassifiedAds.Web.Services;

public enum ModerationSeverity { Clean = 0, Review = 1, Block = 2 }

public record ModerationResult(ModerationSeverity Severity, IReadOnlyList<string> Reasons)
{
    public bool NeedsReview => Severity != ModerationSeverity.Clean;
    public string? Note => Reasons.Count == 0 ? null
        : $"{Severity.ToString().ToUpperInvariant()}: {string.Join(", ", Reasons)}";
}

/// <summary>
/// Rule-based content filter for listing text. Two tiers:
///  • <b>Block</b> — dangerous/illegal signals (weapons, drugs, fake documents, stolen
///    cards/fraud, human/organ, counterfeit) → always held for admin review.
///  • <b>Review</b> — profanity/abuse → held for admin review.
/// Text is lower-cased, de-diacriticized and de-leetspeaked before matching so common
/// evasions (0 for o, @ for a, spacing) still hit. The word lists are the extension
/// point — swap in / augment with an AI/LLM classifier without changing callers.
/// </summary>
public partial class ContentModerationService
{
    // Illegal / dangerous — any hit holds the listing (Block).
    private static readonly string[] DangerTerms =
    [
        // weapons & explosives
        "arma de foc", "pistol", "pistoale", "revolver", "munitie", "cartuse", "kalasnikov", "ak47",
        "grenada", "exploziv", "dinamita", "tnt", "silencer", "amortizor arma", "firearm", "handgun",
        "ammunition", "explosive", "silahs",
        // drugs
        "cocaina", "heroina", "amfetamina", "metamfetamina", "mdma", "ecstasy", "lsd", "cannabis de vanzare",
        "marijuana de vanzare", "hasis", "droguri", "cocaine", "heroin", "meth for sale",
        // fake documents / identity
        "buletin fals", "pasaport fals", "permis fals", "carte identitate falsa", "diploma falsa",
        "acte false", "fake passport", "fake id", "counterfeit document",
        // stolen cards / fraud
        "card clonat", "carduri clonate", "date de card", "cvv dump", "fullz", "cont bancar furat",
        "stolen card", "skimmer", "carding",
        // counterfeit money / goods
        "bani falsi", "counterfeit money", "replica rolex", "ceas replica", "geanta replica",
        // human / organ
        "organe de vanzare", "rinichi de vanzare", "kidney for sale", "trafic persoane",
    ];

    // Profanity / abuse — hit flags for review (Review). Kept intentionally short & unambiguous.
    private static readonly string[] ProfanityTerms =
    [
        "pula", "pizda", "muie", "cacat", "fut ", "futu", "curva", "coaie", "sugi",
        "bou", "jegos", "nenorocit", "handicapat",
        "fuck", "shit", "bitch", "asshole", "cunt", "faggot", "nigger",
    ];

    [GeneratedRegex(@"\s+")] private static partial Regex MultiSpace();

    public ModerationResult Analyze(string? title, string? description)
    {
        var text = Normalize($" {title} {description} ");

        var dangerHits = DangerTerms.Where(t => text.Contains(Normalize($" {t} ").Trim())).Distinct().ToList();
        if (dangerHits.Count > 0)
            return new ModerationResult(ModerationSeverity.Block, dangerHits);

        var profanityHits = ProfanityTerms.Where(t => text.Contains(Normalize($" {t} ").Trim())).Distinct().ToList();
        if (profanityHits.Count > 0)
            return new ModerationResult(ModerationSeverity.Review, profanityHits);

        return new ModerationResult(ModerationSeverity.Clean, []);
    }

    /// <summary>lowercase → strip diacritics → undo common leetspeak → collapse spaces.</summary>
    private static string Normalize(string s)
    {
        var lower = (s ?? "").ToLowerInvariant();
        var deLeet = lower
            .Replace('0', 'o').Replace('1', 'i').Replace('3', 'e')
            .Replace('4', 'a').Replace('5', 's').Replace('7', 't')
            .Replace('@', 'a').Replace('$', 's');
        var decomposed = deLeet.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return MultiSpace().Replace(sb.ToString(), " ");
    }
}
