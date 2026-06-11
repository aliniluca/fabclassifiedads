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

    public List<Listing> Listings { get; set; } = [];
    public List<Favorite> Favorites { get; set; } = [];
}
