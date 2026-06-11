using FabClassifiedAds.Web.Models.Entities;
using FabClassifiedAds.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace FabClassifiedAds.Web.Models;

public class HomeViewModel
{
    public List<Category> RootCategories { get; set; } = [];
    public List<Listing> FeaturedListings { get; set; } = [];
    public List<Listing> LatestListings { get; set; } = [];
    public int TotalActiveListings { get; set; }
    public int TotalUsers { get; set; }
    public int TotalCategories { get; set; }
    public List<CarBrand> PopularBrands { get; set; } = [];
}

public class SearchPageViewModel
{
    public SearchFilters Filters { get; set; } = new();
    public SearchResult Result { get; set; } = new();
    public Category? CurrentCategory { get; set; }
    public List<Category> Breadcrumb { get; set; } = [];
    public List<Category> ChildCategories { get; set; } = [];
    public List<CarBrand> Brands { get; set; } = [];
    public List<CarModel> Models { get; set; } = [];
    public bool ShowCarFilters { get; set; }
    public bool ShowRealEstateFilters { get; set; }
    public HashSet<int> FavoriteIds { get; set; } = [];
}

public class ListingDetailViewModel
{
    public Listing Listing { get; set; } = null!;
    public List<Category> Breadcrumb { get; set; } = [];
    public List<Listing> SimilarListings { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool IsOwner { get; set; }
}

public class CreateListingViewModel
{
    [Required, StringLength(120, MinimumLength = 8)]
    public string Title { get; set; } = "";

    [Required, StringLength(8000, MinimumLength = 20)]
    public string Description { get; set; } = "";

    [Range(0, 100_000_000)]
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsNegotiable { get; set; } = true;
    public bool IsFree { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required, StringLength(80)]
    public string City { get; set; } = "";
    [StringLength(80)]
    public string Region { get; set; } = "";

    public ItemCondition Condition { get; set; } = ItemCondition.Used;

    /// <summary>Uploaded photos (JPEG/PNG/WebP/GIF, max 10 × 5 MB).</summary>
    public List<IFormFile>? Photos { get; set; }

    // car fields
    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
    public string? Variant { get; set; }
    public int? Year { get; set; }
    public int? Mileage { get; set; }
    public FuelType? Fuel { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyType? Body { get; set; }
    public Drivetrain? Drive { get; set; }
    public int? EngineCc { get; set; }
    public int? PowerHp { get; set; }
    public string? Color { get; set; }
    public List<CarOptions> Options { get; set; } = [];

    // real estate fields
    public PropertyType? PropertyType { get; set; }
    public TransactionType? Transaction { get; set; }
    public int? Rooms { get; set; }
    public double? SurfaceM2 { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? YearBuilt { get; set; }
    public HeatingType? Heating { get; set; }
    public FurnishedState? Furnished { get; set; }
    public List<PropertyAmenities> Amenities { get; set; } = [];

    // populated for the view
    public List<Category> CategoryTree { get; set; } = [];
    public List<CarBrand> Brands { get; set; } = [];
}

public class RegisterViewModel
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string DisplayName { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
    public string? City { get; set; }
    public bool IsBusiness { get; set; }
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; } = true;
    public string? ReturnUrl { get; set; }
}
