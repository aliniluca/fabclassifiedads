using FabClassifiedAds.Web.Helpers;
using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db,
    Translator t) : Controller
{
    private static readonly string[] AvatarColors = ["#6d5dfc", "#fc5d8d", "#28c76f", "#ff9f43", "#00cfe8", "#ea5455"];

    [HttpGet("/register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!vm.AcceptTerms)
            ModelState.AddModelError(nameof(vm.AcceptTerms), "You must accept the Terms and Privacy Policy.");
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email, Email = vm.Email,
            DisplayName = vm.DisplayName, City = vm.City, IsBusiness = vm.IsBusiness,
            PhoneNumber = PhoneNumbers.TryNormalize(vm.Phone, out var phone) ? phone : null,
            AvatarColor = AvatarColors[Math.Abs(vm.Email.GetHashCode()) % AvatarColors.Length],
        };
        var result = await userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(vm);
        }
        await signInManager.SignInAsync(user, isPersistent: true);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var account = await userManager.FindByEmailAsync(vm.Email);
        if (account is { IsBanned: true })
        {
            ModelState.AddModelError("", "This account is suspended.");
            return View(vm);
        }

        var result = await signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }
        return LocalRedirect(vm.ReturnUrl is { Length: > 0 } && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : "/");
    }

    [Authorize]
    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet("/account/api")]
    public async Task<IActionResult> ApiKey()
    {
        var user = await userManager.GetUserAsync(User);
        ViewBag.HasKey = user?.ApiKeyHash != null;
        ViewBag.CreatedAt = user?.ApiKeyCreatedAt;
        ViewBag.NewKey = TempData["NewApiKey"] as string;   // shown once, right after generation
        return View();
    }

    [Authorize]
    [HttpPost("/account/api/generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateApiKey()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var raw = Services.ApiKeyService.Generate();
        user.ApiKeyHash = Services.ApiKeyService.Hash(raw);
        user.ApiKeyCreatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        TempData["NewApiKey"] = raw;                          // surfaced once on the next page load
        return RedirectToAction(nameof(ApiKey));
    }

    [Authorize]
    [HttpPost("/account/api/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeApiKey()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        user.ApiKeyHash = null;
        user.ApiKeyCreatedAt = null;
        await userManager.UpdateAsync(user);
        TempData["Flash"] = "API key revoked.";
        return RedirectToAction(nameof(ApiKey));
    }

    // ---------- "Contul meu": profile, email, password ----------

    [Authorize]
    [HttpGet("/account")]
    public async Task<IActionResult> Settings()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(BuildSettings(user));
    }

    [Authorize]
    [HttpPost("/account/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] ProfileForm form)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!ModelState.IsValid) return View("Settings", BuildSettings(user, profile: form));

        var oldPhone = user.PhoneNumber;
        user.DisplayName = form.DisplayName.Trim();
        user.City = string.IsNullOrWhiteSpace(form.City) ? null : form.City.Trim();
        user.IsBusiness = form.IsBusiness;
        if (PhoneNumbers.TryNormalize(form.Phone, out var phone) && phone != oldPhone)
        {
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = false;
            // ads that showed the old number now show the new one
            await db.Listings.Where(l => l.UserId == user.Id && l.ContactPhone != null)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.ContactPhone, phone));
        }
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        TempData["Flash"] = "Your details were saved.";
        return RedirectToAction(nameof(Settings));
    }

    [Authorize]
    [HttpPost("/account/email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail([Bind(Prefix = "EmailChange")] EmailForm form)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var newEmail = form.NewEmail?.Trim() ?? "";

        if (ModelState.IsValid && !await userManager.CheckPasswordAsync(user, form.CurrentPassword))
            ModelState.AddModelError("EmailChange.CurrentPassword", t["The current password is not correct."]);
        if (ModelState.IsValid && await userManager.FindByEmailAsync(newEmail) is { } other && other.Id != user.Id)
            ModelState.AddModelError("EmailChange.NewEmail", t["This email is already used by another account."]);
        if (!ModelState.IsValid) return View("Settings", BuildSettings(user, email: form));

        // the email is also the login name
        user.Email = newEmail;
        user.UserName = newEmail;
        user.EmailConfirmed = false;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View("Settings", BuildSettings(user, email: form));
        }
        await userManager.UpdateSecurityStampAsync(user);   // signs out other sessions
        await signInManager.RefreshSignInAsync(user);
        TempData["Flash"] = "Your email was changed. Use the new address to sign in.";
        return RedirectToAction(nameof(Settings));
    }

    [Authorize]
    [HttpPost("/account/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordChange")] PasswordForm form)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!ModelState.IsValid) return View("Settings", BuildSettings(user));

        var result = await userManager.ChangePasswordAsync(user, form.CurrentPassword, form.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Code == "PasswordMismatch" ? t["The current password is not correct."] : e.Description);
            return View("Settings", BuildSettings(user));
        }
        await signInManager.RefreshSignInAsync(user);   // keeps this session; the new security stamp ends the others
        TempData["Flash"] = "Your password was changed.";
        return RedirectToAction(nameof(Settings));
    }

    private static AccountSettingsViewModel BuildSettings(ApplicationUser user, ProfileForm? profile = null, EmailForm? email = null) => new()
    {
        CurrentEmail = user.Email ?? "",
        MemberSince = user.CreatedAt,
        HasApiKey = user.ApiKeyHash != null,
        Profile = profile ?? new ProfileForm
        {
            DisplayName = user.DisplayName,
            Phone = PhoneNumbers.Format(user.PhoneNumber),
            City = user.City,
            IsBusiness = user.IsBusiness,
        },
        EmailChange = email ?? new EmailForm(),
    };

    [Authorize]
    [HttpGet("/account/data")]
    public IActionResult Data() => View();

    /// <summary>GDPR data portability: download everything we hold about the user as JSON.</summary>
    [Authorize]
    [HttpGet("/account/data-export")]
    public async Task<IActionResult> DataExport()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();

        var export = new
        {
            exportedAt = DateTime.UtcNow,
            account = new { user.DisplayName, user.Email, user.City, user.IsBusiness, user.CreatedAt },
            listings = await db.Listings.AsNoTracking().Where(l => l.UserId == userId)
                .Select(l => new { l.Id, l.Title, l.Description, l.Price, l.Currency, l.City, l.Status, l.CreatedAt })
                .ToListAsync(),
            favorites = await db.Favorites.AsNoTracking().Where(f => f.UserId == userId)
                .Select(f => new { f.ListingId, f.CreatedAt }).ToListAsync(),
            savedSearches = await db.SavedSearches.AsNoTracking().Where(s => s.UserId == userId)
                .Select(s => new { s.Name, s.QueryString, s.EmailAlerts, s.CreatedAt }).ToListAsync(),
            messages = await db.Messages.AsNoTracking().Where(m => m.SenderId == userId)
                .Select(m => new { m.ConversationId, m.Body, m.SentAt }).ToListAsync(),
        };

        var json = System.Text.Json.JsonSerializer.Serialize(export,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json",
            $"aicigasesti-data-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>GDPR right to erasure: delete the account and all associated personal data.</summary>
    [Authorize]
    [HttpPost("/account/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string confirm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();

        if (!string.Equals(confirm?.Trim(), "DELETE", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(confirm?.Trim(), "ȘTERGE", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Flash"] = "Type DELETE to confirm.";
            return RedirectToAction(nameof(Data));
        }

        // conversations reference the user with Restrict — remove them first (cascades messages),
        // then the user delete cascades listings, favorites and saved searches
        var conversations = await db.Conversations
            .Where(c => c.BuyerId == userId || c.SellerId == userId).ToListAsync();
        db.Conversations.RemoveRange(conversations);
        await db.SaveChangesAsync();

        await signInManager.SignOutAsync();
        await userManager.DeleteAsync(user);

        TempData["Flash"] = "Your account and data were deleted.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet("/my-ads")]
    public async Task<IActionResult> MyAds()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var listings = await db.Listings.AsNoTracking()
            .Where(l => l.UserId == userId)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.RealEstateDetails)
            .OrderByDescending(l => l.CreatedAt).ToListAsync();
        return View(listings);
    }
}
