using System.Security.Cryptography;
using System.Text;
using FabClassifiedAds.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

/// <summary>Who a presented API key belongs to. <see cref="IsMaster"/> = the server-wide admin key.</summary>
public record ApiKeyResolution(string? UserId, bool IsMaster);

/// <summary>
/// Resolves API keys to accounts. The master key (env <c>IMPORT_API_KEY</c> / <c>Api:Key</c>)
/// grants admin access; otherwise the key is hashed and matched against a user's
/// <see cref="Models.Entities.ApplicationUser.ApiKeyHash"/>. Only the hash is stored — the
/// raw key is shown to the owner once at generation.
/// </summary>
public class ApiKeyService(AppDbContext db, IConfiguration config)
{
    public const string KeyPrefix = "agk_";   // AiciGăsești key

    public static string Hash(string rawKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

    /// <summary>Create a new random key (returned raw; caller stores only the hash).</summary>
    public static string Generate() =>
        KeyPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public async Task<ApiKeyResolution?> ResolveAsync(string? provided)
    {
        if (string.IsNullOrWhiteSpace(provided)) return null;

        var master = MasterKey(config);
        if (!string.IsNullOrEmpty(master) &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(master)))
            return new ApiKeyResolution(null, IsMaster: true);

        var hash = Hash(provided);
        var userId = await db.Users.AsNoTracking()
            .Where(u => u.ApiKeyHash == hash)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        return userId is null ? null : new ApiKeyResolution(userId, IsMaster: false);
    }

    public static string? MasterKey(IConfiguration config) =>
        config["Api:Key"]
        ?? Environment.GetEnvironmentVariable("IMPORT_API_KEY")
        ?? config["Import:ApiKey"];
}
