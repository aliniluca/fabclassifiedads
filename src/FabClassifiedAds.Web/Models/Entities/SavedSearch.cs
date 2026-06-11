namespace FabClassifiedAds.Web.Models.Entities;

public class SavedSearch
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = "";
    /// <summary>Original query string, used to re-run the search in the UI.</summary>
    public string QueryString { get; set; } = "";
    /// <summary>Serialized SearchFilters, used by the alert service to find new matches.</summary>
    public string FiltersJson { get; set; } = "{}";

    public bool EmailAlerts { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Listings created after this moment are considered "new" for alerts.</summary>
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
}
