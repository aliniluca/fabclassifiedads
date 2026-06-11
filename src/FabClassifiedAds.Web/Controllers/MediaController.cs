using Microsoft.AspNetCore.Mvc;

namespace FabClassifiedAds.Web.Controllers;

/// <summary>
/// Serves deterministic gradient SVG placeholders so demo listings always have imagery
/// without external dependencies. Real photo upload can replace this later.
/// </summary>
public class MediaController : Controller
{
    private static readonly (string From, string To)[] Palettes =
    [
        ("#667eea", "#764ba2"), ("#f093fb", "#f5576c"), ("#4facfe", "#00f2fe"),
        ("#43e97b", "#38f9d7"), ("#fa709a", "#fee140"), ("#30cfd0", "#330867"),
        ("#a8edea", "#fed6e3"), ("#ff9a9e", "#fecfef"), ("#fbc2eb", "#a6c1ee"),
        ("#fdcbf1", "#e6dee9"), ("#a1c4fd", "#c2e9fb"), ("#d4fc79", "#96e6a1"),
        ("#84fab0", "#8fd3f4"), ("#cfd9df", "#e2ebf0"), ("#fccb90", "#d57eeb"),
        ("#e0c3fc", "#8ec5fc"), ("#f5f7fa", "#c3cfe2"), ("#fad0c4", "#ffd1ff"),
    ];

    [HttpGet("/media/ph/{seed}.svg")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Placeholder(string seed)
    {
        var hash = Math.Abs(seed.GetHashCode(StringComparison.Ordinal));
        var (from, to) = Palettes[hash % Palettes.Length];
        var angle = hash % 360;
        var initial = char.ToUpperInvariant(seed.FirstOrDefault(char.IsLetter) is var c && c != '\0' ? c : 'F');

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
              <defs>
                <linearGradient id="g" gradientTransform="rotate({angle} 0.5 0.5)">
                  <stop offset="0%" stop-color="{from}"/>
                  <stop offset="100%" stop-color="{to}"/>
                </linearGradient>
              </defs>
              <rect width="800" height="600" fill="url(#g)"/>
              <circle cx="{120 + hash % 560}" cy="{100 + hash % 400}" r="180" fill="#ffffff" opacity="0.10"/>
              <circle cx="{600 - hash % 400}" cy="{450 - hash % 300}" r="120" fill="#000000" opacity="0.07"/>
              <text x="400" y="330" font-family="Arial, sans-serif" font-size="160" font-weight="bold"
                    fill="#ffffff" opacity="0.35" text-anchor="middle">{initial}</text>
            </svg>
            """;
        return Content(svg, "image/svg+xml");
    }
}
