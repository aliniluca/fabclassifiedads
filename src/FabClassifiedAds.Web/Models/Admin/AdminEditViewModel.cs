using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Models.Admin;

/// <summary>Admin edit form for any listing (all core fields + status + images).</summary>
public class AdminEditViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsNegotiable { get; set; }
    public bool IsFree { get; set; }
    public int CategoryId { get; set; }
    public string City { get; set; } = "";
    public string? Region { get; set; }
    public ItemCondition Condition { get; set; }
    public ListingStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsUrgent { get; set; }
    public string? ModerationNote { get; set; }
    public string? VideoUrl { get; set; }

    // car
    public bool HasCar { get; set; }
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

    // real estate
    public bool HasRealEstate { get; set; }
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

    // images
    public List<ListingImage> ExistingImages { get; set; } = [];
    public List<int> RemoveImageIds { get; set; } = [];
    public string? AddImageUrls { get; set; }   // newline-separated

    // populated for the view
    public List<Category> CategoryTree { get; set; } = [];
    public List<CarBrand> Brands { get; set; } = [];
}
