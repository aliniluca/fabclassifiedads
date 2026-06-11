using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FabClassifiedAds.Web.Controllers;

public class LanguageController : Controller
{
    [HttpGet("/set-language")]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (culture is not ("ro" or "en")) culture = "ro";

        // Only the UI culture varies — the formatting culture stays en-US so that
        // form values like "52.5" always model-bind the same way.
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US", culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
