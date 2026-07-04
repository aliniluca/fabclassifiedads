using Microsoft.AspNetCore.Identity;

namespace FabClassifiedAds.Web.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string? AvatarColor { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsBusiness { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }

    /// <summary>SHA-256 of this account's personal API key (raw key is shown once, never stored).</summary>
    public string? ApiKeyHash { get; set; }
    public DateTime? ApiKeyCreatedAt { get; set; }

    /// <summary>Granted from the admin Users tab (in addition to the ADMIN_EMAILS allow-list).</summary>
    public bool IsAdmin { get; set; }
    /// <summary>Banned users can't sign in or post.</summary>
    public bool IsBanned { get; set; }

    public List<Listing> Listings { get; set; } = [];
    public List<Favorite> Favorites { get; set; } = [];
}
