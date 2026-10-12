using System.Net;
using System.Text;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Account emails with a confirmation link: confirm the address after registration, and
/// confirm a new address before an email change takes effect. Tokens come from ASP.NET Identity
/// (single use, tied to the user's security stamp) and travel Base64Url-encoded in the link.
/// A failed send never breaks the calling flow — it returns false and the user can resend.
/// </summary>
public class AccountEmails(
    UserManager<ApplicationUser> userManager,
    IEmailSender email,
    ILogger<AccountEmails> logger)
{
    public async Task<bool> SendConfirmationAsync(ApplicationUser user, string baseUrl)
    {
        var token = Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
        var link = $"{baseUrl}/account/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={token}";
        var ro = Translator.IsRo;
        return await SendAsync(user.Email!,
            ro ? "Confirmă adresa de email — AiciGăsești" : "Confirm your email — AiciGăsești",
            ro ? $"Bună, {user.DisplayName}!" : $"Hi {user.DisplayName}!",
            ro ? "Mulțumim că ți-ai făcut cont pe AiciGăsești. Confirmă adresa de email ca să-ți crească scorul de încredere ca vânzător."
               : "Thanks for joining AiciGăsești. Confirm your email address to raise your seller trust score.",
            ro ? "Confirmă emailul" : "Confirm email", link);
    }

    public async Task<bool> SendEmailChangeAsync(ApplicationUser user, string newEmail, string baseUrl)
    {
        var token = Encode(await userManager.GenerateChangeEmailTokenAsync(user, newEmail));
        var link = $"{baseUrl}/account/confirm-email-change?userId={Uri.EscapeDataString(user.Id)}" +
                   $"&email={Uri.EscapeDataString(newEmail)}&token={token}";
        var ro = Translator.IsRo;
        return await SendAsync(newEmail,
            ro ? "Confirmă noua adresă de email — AiciGăsești" : "Confirm your new email — AiciGăsești",
            ro ? $"Bună, {user.DisplayName}!" : $"Hi {user.DisplayName}!",
            ro ? $"Ai cerut ca adresa contului tău AiciGăsești să devină {newEmail}. Schimbarea se aplică după ce confirmi. Până atunci te conectezi cu adresa veche."
               : $"You asked to change your AiciGăsești email to {newEmail}. The change applies once you confirm; until then keep signing in with your old address.",
            ro ? "Confirmă noua adresă" : "Confirm new address", link);
    }

    public async Task<bool> SendPasswordResetAsync(ApplicationUser user, string baseUrl)
    {
        var token = Encode(await userManager.GeneratePasswordResetTokenAsync(user));
        var link = $"{baseUrl}/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={token}";
        var ro = Translator.IsRo;
        return await SendAsync(user.Email!,
            ro ? "Resetează parola — AiciGăsești" : "Reset your password — AiciGăsești",
            ro ? $"Bună, {user.DisplayName}!" : $"Hi {user.DisplayName}!",
            ro ? "Am primit o cerere de resetare a parolei pentru contul tău. Linkul de mai jos e valabil o singură dată."
               : "We received a request to reset your password. The link below works once.",
            ro ? "Alege o parolă nouă" : "Choose a new password", link);
    }

    public static string Decode(string token)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return ""; }
    }

    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private async Task<bool> SendAsync(string to, string subject, string greeting, string text, string button, string link)
    {
        var ro = Translator.IsRo;
        var ignore = ro ? "Dacă nu tu ai cerut asta, ignoră acest email." : "If you didn't ask for this, ignore this email.";
        var fallback = ro ? "Dacă butonul nu merge, copiază linkul în browser:" : "If the button doesn't work, paste this link into your browser:";
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;background:#f3f5f7;padding:24px">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border:1px solid #d6dce3;border-radius:6px">
                <div style="background:#13233a;color:#ffffff;padding:16px 24px;font-size:20px;font-weight:bold;border-radius:6px 6px 0 0">AiciGăsești</div>
                <div style="padding:24px;color:#13233a;font-size:15px;line-height:1.5">
                  <p style="margin:0 0 12px">{WebUtility.HtmlEncode(greeting)}</p>
                  <p style="margin:0 0 20px">{WebUtility.HtmlEncode(text)}</p>
                  <p style="margin:0 0 24px"><a href="{link}" style="display:inline-block;background:#0b6b4f;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 22px;border-radius:6px">{WebUtility.HtmlEncode(button)}</a></p>
                  <p style="margin:0 0 6px;color:#556274;font-size:13px">{WebUtility.HtmlEncode(fallback)}</p>
                  <p style="margin:0 0 20px;font-size:13px;word-break:break-all"><a href="{link}" style="color:#0b6b4f">{WebUtility.HtmlEncode(link)}</a></p>
                  <p style="margin:0;color:#556274;font-size:13px">{WebUtility.HtmlEncode(ignore)}</p>
                </div>
              </div>
            </div>
            """;
        try
        {
            await email.SendAsync(to, subject, html);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not send account email to {To}", to);
            return false;
        }
    }
}
