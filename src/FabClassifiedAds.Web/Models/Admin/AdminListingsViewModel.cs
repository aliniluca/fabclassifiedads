using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Models.Admin;

/// <summary>Moderation table: one filterable, sortable, paged view over every listing.</summary>
public class AdminListingsViewModel
{
    public List<Listing> Listings { get; set; } = [];
    public List<Category> RootCategories { get; set; } = [];

    // filters (echoed back into the form and the paging links)
    public string Tab { get; set; } = "pending";
    public string? Q { get; set; }
    public string? Category { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public AdminCounts Counts { get; set; } = new();
}

public class AdminCounts
{
    public int Pending { get; set; }
    public int Active { get; set; }
    public int Rejected { get; set; }
    public int Expired { get; set; }
    public int Suspended { get; set; }
    public int Risky { get; set; }
    public int All { get; set; }
    public int NewToday { get; set; }
    public int Users { get; set; }
}
