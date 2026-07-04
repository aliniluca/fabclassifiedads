using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Admin identity. Bootstrap admins come from the <c>ADMIN_EMAILS</c> env var /
/// <c>Admin:Emails</c> config; additional admins are granted from the Users tab
/// (the <c>IsAdmin</c> flag). Both are surfaced as the "Admin" role via a claims
/// transformation, so the rest of the app just checks <c>User.IsInRole("Admin")</c>.
/// </summary>
public static class AdminAccess
{
    public const string PolicyName = "Admin";
    public const string Role = "Admin";

    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(Role);

    public static IReadOnlySet<string> BootstrapEmails(IConfiguration config)
    {
        var fromArray = config.GetSection("Admin:Emails").Get<string[]>() ?? [];
        var raw = string.Join(",",
            fromArray.Append(config["Admin:Emails"] ?? "")
                     .Append(Environment.GetEnvironmentVariable("ADMIN_EMAILS") ?? ""));
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();
    }

    public static bool IsBootstrapAdmin(ClaimsPrincipal user, IConfiguration config)
    {
        var emails = BootstrapEmails(config);
        if (emails.Count == 0) return false;
        var candidates = new[]
        {
            user.FindFirstValue(ClaimTypes.Email),
            user.FindFirstValue(ClaimTypes.Name),
            user.Identity?.Name,
        };
        return candidates.Any(c => c != null && emails.Contains(c.ToLowerInvariant()));
    }
}

/// <summary>Adds the "Admin" role claim per request for bootstrap-email or DB-flagged admins.</summary>
public class AdminClaimsTransformation(IServiceScopeFactory scopeFactory, IConfiguration config) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true } || principal.IsInRole(AdminAccess.Role))
            return principal;

        var isAdmin = AdminAccess.IsBootstrapAdmin(principal, config);
        if (!isAdmin)
        {
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id != null)
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                isAdmin = await db.Users.Where(u => u.Id == id).Select(u => u.IsAdmin).FirstOrDefaultAsync();
            }
        }

        if (isAdmin && principal.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(ClaimTypes.Role, AdminAccess.Role));
        return principal;
    }
}
