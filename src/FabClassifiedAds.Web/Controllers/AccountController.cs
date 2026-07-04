using System.Security.Claims;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db) : Controller
{
    private static readonly string[] AvatarColors = ["#6d5dfc", "#fc5d8d", "#28c76f", "#ff9f43", "#00cfe8", "#ea5455"];

    [HttpGet("/register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email, Email = vm.Email,
            DisplayName = vm.DisplayName, City = vm.City, IsBusiness = vm.IsBusiness,
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
