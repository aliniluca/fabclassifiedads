using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FabClassifiedAds.Web.Services;

namespace FabClassifiedAds.Web.Helpers;

/// <summary>
/// Phone validation + normalisation. Romanian numbers (07xx mobile, 02x/03x landline; with or
/// without +40 / 0040) are stored as E.164 "+40712345678"; foreign numbers must be written
/// in international form ("+373 69 123 456") and are stored as "+37369123456".
/// </summary>
public static partial class PhoneNumbers
{
    [GeneratedRegex(@"[\s\.\-\(\)/]")]
    private static partial Regex Separators();

    [GeneratedRegex(@"^0(7\d{8}|[23]\d{8})$")]
    private static partial Regex RoNational();

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex International();

    public static bool TryNormalize(string? input, out string e164)
    {
        e164 = "";
        if (string.IsNullOrWhiteSpace(input)) return false;
        var s = Separators().Replace(input.Trim(), "");
        if (s.StartsWith("00")) s = "+" + s[2..];

        // Romanian: +40 7xx… / 07xx… (also tolerates the common "+400 7xx…" typo)
        var national = s.StartsWith("+40") ? "0" + s[3..].TrimStart('0') : s;
        if (RoNational().IsMatch(national))
        {
            e164 = "+40" + national[1..];
            return true;
        }
        if (!s.StartsWith("+40") && International().IsMatch(s))
        {
            e164 = s;
            return true;
        }
        return false;
    }

    /// <summary>Readable form for display: "+40712345678" -> "0712 345 678"; others unchanged.</summary>
    public static string Format(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        if (stored.StartsWith("+40") && stored.Length == 12)
        {
            var n = "0" + stored[3..];
            return $"{n[..4]} {n[4..7]} {n[7..]}";
        }
        return stored;
    }
}

/// <summary>Mandatory, valid phone number (see <see cref="PhoneNumbers.TryNormalize"/>); localised message.</summary>
public sealed class ValidPhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        PhoneNumbers.TryNormalize(value as string, out _);   // empty is invalid too: the phone is mandatory

    public override string FormatErrorMessage(string name) => Translator.IsRo
        ? "Introdu un număr de telefon valid, de exemplu 0712 345 678 (sau +cod țară pentru numere din străinătate)."
        : "Enter a valid phone number, e.g. 0712 345 678 (or +country code for numbers abroad).";
}
