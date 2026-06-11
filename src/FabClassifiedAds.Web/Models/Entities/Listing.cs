using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabClassifiedAds.Web.Models.Entities;

public class Listing
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = "";

    [MaxLength(8000)]
    public string Description { get; set; } = "";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    public string Currency { get; set; } = "EUR";
    public bool IsNegotiable { get; set; }
    public bool IsFree { get; set; }
    public bool IsExchange { get; set; }

    public ItemCondition Condition { get; set; } = ItemCondition.Used;
    public SellerType SellerType { get; set; } = SellerType.Private;

    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public bool IsFeatured { get; set; }
    public bool IsUrgent { get; set; }
    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime BumpedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Optional vertical video clip shown in the swipe feed.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>0-100 trust score computed by TrustService when the ad is published.</summary>
    public int TrustScore { get; set; } = 100;

    public List<ListingImage> Images { get; set; } = [];
    public List<ListingAttribute> Attributes { get; set; } = [];
    public CarDetails? CarDetails { get; set; }
    public RealEstateDetails? RealEstateDetails { get; set; }
    public List<Favorite> Favorites { get; set; } = [];
}

public enum ItemCondition { New = 0, Used = 1, Refurbished = 2, ForParts = 3 }
public enum SellerType { Private = 0, Business = 1 }
public enum ListingStatus { Draft = 0, Active = 1, Sold = 2, Expired = 3, Suspended = 4 }

public class ListingImage
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}

/// <summary>Generic key/value attribute for categories without specialized details.</summary>
public class ListingAttribute
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    [MaxLength(60)] public string Key { get; set; } = "";
    [MaxLength(255)] public string Value { get; set; } = "";
}

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
