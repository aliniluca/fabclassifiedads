using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Guards an API controller/action with a shared secret. The key is read from
/// config <c>Api:Key</c> or the <c>IMPORT_API_KEY</c> environment variable; if none
/// is configured the endpoint stays locked (401). Callers send the key in the
/// <c>X-Api-Key</c> header (<c>X-Import-Key</c> is also accepted for continuity).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public static string? ConfiguredKey(IConfiguration config) =>
        config["Api:Key"]
        ?? Environment.GetEnvironmentVariable("IMPORT_API_KEY")
        ?? config["Import:ApiKey"];

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = ConfiguredKey(config);
        if (string.IsNullOrEmpty(expected))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var req = context.HttpContext.Request;
        var provided = req.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(provided)) provided = req.Headers["X-Import-Key"].ToString();

        if (string.IsNullOrEmpty(provided) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
