using System.Security.Claims;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Admins are identified by email allow-list, from <c>Admin:Emails</c> (config, array or
/// comma-separated) or the <c>ADMIN_EMAILS</c> environment variable. No role tables needed.
/// </summary>
public static class AdminAccess
{
    public const string PolicyName = "Admin";

    public static IReadOnlySet<string> Emails(IConfiguration config)
    {
        var fromArray = config.GetSection("Admin:Emails").Get<string[]>() ?? [];
        var raw = string.Join(",",
            fromArray.Append(config["Admin:Emails"] ?? "")
                     .Append(Environment.GetEnvironmentVariable("ADMIN_EMAILS") ?? ""));
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
    }

    public static bool IsAdmin(ClaimsPrincipal user, IConfiguration config)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        var emails = Emails(config);
        if (emails.Count == 0) return false;
        var candidates = new[]
        {
            user.FindFirstValue(ClaimTypes.Email),
            user.FindFirstValue(ClaimTypes.Name),
            user.Identity.Name,
        };
        return candidates.Any(c => c != null && emails.Contains(c.ToLowerInvariant()));
    }
}
