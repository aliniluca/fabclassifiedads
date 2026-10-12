using Microsoft.AspNetCore.Html;

namespace FabClassifiedAds.Web.Helpers;

/// <summary>
/// Inline line-icon set (24px grid, 1.5px stroke, currentColor) — the site's only icon system.
/// Paths follow the Lucide icon geometry (ISC licence).
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["search"] = "<circle cx='11' cy='11' r='7'/><path d='m20 20-3.5-3.5'/>",
        ["pin"] = "<path d='M20 10c0 5-8 12-8 12s-8-7-8-12a8 8 0 0 1 16 0Z'/><circle cx='12' cy='10' r='3'/>",
        ["heart"] = "<path d='M19 14c1.5-1.5 3-3.3 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.8 0-3 .5-4.5 2-1.5-1.5-2.7-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4 3 5.5l7 7Z'/>",
        ["message"] = "<path d='M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z'/>",
        ["bell"] = "<path d='M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9'/><path d='M10.3 21a1.94 1.94 0 0 0 3.4 0'/>",
        ["list"] = "<path d='M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01'/>",
        ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21a8 8 0 0 1 16 0'/>",
        ["plus"] = "<path d='M12 5v14M5 12h14'/>",
        ["volume"] = "<path d='M11 5 6 9H2v6h4l5 4V5Z'/><path d='M15.5 8.5a5 5 0 0 1 0 7M19 5a10 10 0 0 1 0 14'/>",
        ["volume-off"] = "<path d='M11 5 6 9H2v6h4l5 4V5Z'/><path d='m22 9-6 6M16 9l6 6'/>",
        ["shield"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10'/><path d='m9 12 2 2 4-4'/>",
        ["check"] = "<path d='M20 6 9 17l-5-5'/>",
        ["x"] = "<path d='M18 6 6 18M6 6l12 12'/>",
        ["alert"] = "<path d='m21.7 18-8-14a2 2 0 0 0-3.4 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.7-3'/><path d='M12 9v4M12 17h.01'/>",
        ["info"] = "<circle cx='12' cy='12' r='10'/><path d='M12 16v-4M12 8h.01'/>",
        ["video"] = "<path d='m16 13 5.2 3.5a.5.5 0 0 0 .8-.4V7.9a.5.5 0 0 0-.8-.4L16 11'/><rect x='2' y='6' width='14' height='12' rx='2'/>",
        ["camera"] = "<path d='M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3z'/><circle cx='12' cy='13' r='3'/>",
        ["star"] = "<path d='m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8L12 17.8 5.8 21l1.2-6.8-5-4.9 6.9-1z'/>",
        ["link"] = "<path d='M10 13a5 5 0 0 0 7.5.5l3-3a5 5 0 0 0-7-7l-1.7 1.7'/><path d='M14 11a5 5 0 0 0-7.5-.5l-3 3a5 5 0 0 0 7 7l1.7-1.7'/>",
        ["menu"] = "<path d='M4 6h16M4 12h16M4 18h16'/>",
        ["arrow"] = "<path d='M5 12h14M13 6l6 6-6 6'/>",
        ["clock"] = "<circle cx='12' cy='12' r='10'/><path d='M12 6v6l4 2'/>",
        ["phone"] = "<path d='M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8 9.9a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2'/>",
        ["share"] = "<circle cx='18' cy='5' r='3'/><circle cx='6' cy='12' r='3'/><circle cx='18' cy='19' r='3'/><path d='m8.6 13.5 6.8 4M15.4 6.5l-6.8 4'/>",
        ["map"] = "<path d='m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3z'/><path d='M9 3v15M15 6v15'/>",
        ["image"] = "<rect x='3' y='3' width='18' height='18' rx='2'/><circle cx='9' cy='9' r='2'/><path d='m21 15-3.1-3.1a2 2 0 0 0-2.8 0L6 21'/>",
        ["upload"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12'/>",
        ["trash"] = "<path d='M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2'/>",
        ["edit"] = "<path d='M12 20h9M16.4 3.6a2.1 2.1 0 1 1 3 3L7 19l-4 1 1-4z'/>",
        ["eye"] = "<path d='M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12'/><circle cx='12' cy='12' r='3'/>",
        ["settings"] = "<circle cx='12' cy='12' r='3'/><path d='M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1'/>",
        ["logout"] = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9'/>",
        ["globe"] = "<circle cx='12' cy='12' r='10'/><path d='M2 12h20M12 2a15 15 0 0 1 0 20 15 15 0 0 1 0-20'/>",
        ["key"] = "<circle cx='7.5' cy='15.5' r='5.5'/><path d='m21 2-9.6 9.6M15.5 7.5l3 3L22 7l-3-3'/>",
        ["download"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3'/>",
        ["play"] = "<path d='m6 3 14 9-14 9z'/>",
        // category roots
        ["car"] = "<path d='M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9L18.4 10 16 6.6A2 2 0 0 0 14.4 6H8.6a2 2 0 0 0-1.6.8L4.6 10 3.4 10.4A2 2 0 0 0 2 12.3V16c0 .6.4 1 1 1h2'/><circle cx='7' cy='17' r='2'/><circle cx='17' cy='17' r='2'/><path d='M9 17h6'/>",
        ["home"] = "<path d='M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z'/>",
        ["device"] = "<rect x='6' y='2' width='12' height='20' rx='2'/><path d='M11 18h2'/>",
        ["sofa"] = "<path d='M20 9V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v3'/><path d='M2 16a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-5a2 2 0 0 0-4 0v2H6v-2a2 2 0 0 0-4 0z'/><path d='M4 18v2M20 18v2'/>",
        ["shirt"] = "<path d='M20.4 4.5 16 3a4 4 0 0 1-8 0L3.6 4.5a2 2 0 0 0-1.3 2.2l.6 3.5a1 1 0 0 0 1 .8H6v10a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V11h2.1a1 1 0 0 0 1-.8l.6-3.5a2 2 0 0 0-1.3-2.2'/>",
        ["baby"] = "<circle cx='12' cy='12' r='9'/><path d='M9 12h.01M15 12h.01M10 16c.5.3 1.2.5 2 .5s1.5-.2 2-.5'/><path d='M12 3a2 2 0 0 0 0 4'/>",
        ["ball"] = "<circle cx='12' cy='12' r='10'/><path d='M12 2a14.5 14.5 0 0 0 0 20M2 12h20'/><path d='M4.9 4.9a14 14 0 0 1 14.2 0M4.9 19.1a14 14 0 0 0 14.2 0'/>",
        ["paw"] = "<circle cx='11' cy='4' r='2'/><circle cx='18' cy='8' r='2'/><circle cx='20' cy='16' r='2'/><path d='M9 10a5 5 0 0 1 5 5v3.5a3.5 3.5 0 0 1-6.8 1.2A6 6 0 0 0 4.4 17 3 3 0 0 1 4 14a5 5 0 0 1 5-4'/>",
        ["wheat"] = "<path d='M2 22 16 8'/><path d='M3.5 12.5 5 11l1.5 1.5a3.5 3.5 0 0 1 0 5L5 19l-1.5-1.5a3.5 3.5 0 0 1 0-5M7.5 8.5 9 7l1.5 1.5a3.5 3.5 0 0 1 0 5L9 15l-1.5-1.5a3.5 3.5 0 0 1 0-5M11.5 4.5 13 3l1.5 1.5a3.5 3.5 0 0 1 0 5L13 11l-1.5-1.5a3.5 3.5 0 0 1 0-5'/><path d='M20 2h2v2a4 4 0 0 1-4 4h-2V6a4 4 0 0 1 4-4'/>",
        ["briefcase"] = "<rect x='2' y='7' width='20' height='14' rx='2'/><path d='M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16'/>",
        ["wrench"] = "<path d='M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.8-3.8a6 6 0 0 1-7.9 7.9l-6.9 6.9a2.1 2.1 0 0 1-3-3l6.9-6.9a6 6 0 0 1 7.9-7.9z'/>",
        ["tag"] = "<path d='M12.6 2.6A2 2 0 0 0 11.2 2H4a2 2 0 0 0-2 2v7.2a2 2 0 0 0 .6 1.4l8.7 8.7a2.4 2.4 0 0 0 3.4 0l6.6-6.6a2.4 2.4 0 0 0 0-3.4z'/><circle cx='7.5' cy='7.5' r='.5'/>",
    };

    private static readonly Dictionary<string, string> CategoryIcons = new()
    {
        ["auto-moto-and-boats"] = "car",
        ["real-estate"] = "home",
        ["electronics-and-appliances"] = "device",
        ["home-and-garden"] = "sofa",
        ["fashion-and-beauty"] = "shirt",
        ["mom-and-kids"] = "baby",
        ["sports-hobby-and-leisure"] = "ball",
        ["animals"] = "paw",
        ["agriculture-and-industry"] = "wheat",
        ["jobs"] = "briefcase",
        ["services"] = "wrench",
    };

    /// <summary>Decorative inline SVG icon (aria-hidden). Pass a CSS class for sizing.</summary>
    public static IHtmlContent Svg(string name, string? cssClass = null)
    {
        var body = Paths.GetValueOrDefault(name) ?? Paths["tag"];
        var cls = cssClass is null ? "icon" : $"icon {cssClass}";
        return new HtmlString(
            $"<svg class=\"{cls}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" " +
            $"stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{body}</svg>");
    }

    /// <summary>Icon for a root category slug (falls back to a price tag).</summary>
    public static IHtmlContent Category(string slug, string? cssClass = null) =>
        Svg(CategoryIcons.GetValueOrDefault(slug, "tag"), cssClass);
}
