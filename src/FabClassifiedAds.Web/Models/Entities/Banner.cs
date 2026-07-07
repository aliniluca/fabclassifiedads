using System.ComponentModel.DataAnnotations;

namespace FabClassifiedAds.Web.Models.Entities;

public enum BannerPlacement { HomeTop = 0, SearchTop = 1, Sidebar = 2 }

/// <summary>An advertising banner managed from the admin panel.</summary>
public class Banner
{
    public int Id { get; set; }
    [MaxLength(120)] public string Title { get; set; } = "";
    [MaxLength(500)] public string ImageUrl { get; set; } = "";
    [MaxLength(500)] public string LinkUrl { get; set; } = "";
    public BannerPlacement Placement { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLive(DateTime now) =>
        IsActive && (StartsAt is null || StartsAt <= now) && (EndsAt is null || EndsAt >= now);
}
