using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Guards an API controller/action with an API key sent in the <c>X-Api-Key</c> header
/// (<c>X-Import-Key</c> also accepted). The key may be the server master key or any
/// account's personal key. On success the resolved owner is stashed in
/// <c>HttpContext.Items["ApiUserId"]</c> (null = master) for the action to use.
/// Set <see cref="RequireMaster"/> to allow only the master key.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string UserIdItem = "ApiUserId";
    public const string IsMasterItem = "ApiIsMaster";

    public bool RequireMaster { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var req = context.HttpContext.Request;
        var provided = req.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(provided)) provided = req.Headers["X-Import-Key"].ToString();

        var svc = context.HttpContext.RequestServices.GetRequiredService<ApiKeyService>();
        var resolution = await svc.ResolveAsync(provided);

        if (resolution is null || (RequireMaster && !resolution.IsMaster))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[UserIdItem] = resolution.UserId;
        context.HttpContext.Items[IsMasterItem] = resolution.IsMaster;
    }
}
