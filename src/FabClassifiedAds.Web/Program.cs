using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=aicigasesti.db"));

// Trust the X-Forwarded-* headers set by the Nginx reverse proxy (running on
// loopback), so Request.Scheme/RemoteIp are correct behind TLS termination.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";   // 403 (e.g. non-admin) — not a logout
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    // Romanian is the primary UI language, English the secondary. The formatting
    // culture is pinned to en-US so number/date model binding never changes.
    var ui = new[] { new System.Globalization.CultureInfo("ro"), new System.Globalization.CultureInfo("en") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US", "ro");
    options.SupportedCultures = [new System.Globalization.CultureInfo("en-US")];
    options.SupportedUICultures = ui;
    options.RequestCultureProviders = [new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()];
});

builder.Services.AddSingleton<Translator>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<PhotoStorage>();
builder.Services.AddScoped<TrustService>();
builder.Services.AddSingleton<SafeHttpFetcher>();
builder.Services.AddScoped<ListingUrlImporter>();
builder.Services.AddScoped<CategoryDetector>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddSingleton<SettingsStore>();
builder.Services.AddSingleton<ContentModerationService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, AdminClaimsTransformation>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAccess.PolicyName, policy => policy.RequireRole(AdminAccess.Role));
builder.Services.AddSingleton<IEmailSender, OutboxEmailSender>();
builder.Services.AddHostedService<SavedSearchAlertService>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseForwardedHeaders();

// baseline security response headers
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "SAMEORIGIN";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["X-Permitted-Cross-Domain-Policies"] = "none";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   // attribute-routed API (e.g. /api/import/*)
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    // ensure tables added after the first deploy exist on already-provisioned DBs
    await ImportSchema.EnsureAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.Run();
