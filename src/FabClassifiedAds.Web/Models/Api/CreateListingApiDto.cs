using System.ComponentModel.DataAnnotations;
using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Models.Api;

/// <summary>Public payload for programmatically creating a listing (POST /api/listings).</summary>
public class CreateListingApiDto
{
    [Required, StringLength(120, MinimumLength = 8)] public string Title { get; set; } = "";
    [Required, StringLength(8000, MinimumLength = 10)] public string Description { get; set; } = "";

    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsNegotiable { get; set; } = true;
    public bool IsFree { get; set; }

    /// <summary>Category slug (see GET /api/categories). If omitted, it is auto-detected from the text.</summary>
    public string? CategorySlug { get; set; }

    [Required] public string City { get; set; } = "";
    public string? Region { get; set; }
    public ItemCondition Condition { get; set; } = ItemCondition.Used;

    /// <summary>Publish live (Active) or keep hidden (Draft). Defaults to Active.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Absolute image URLs; downloaded server-side (SSRF-guarded) on create.</summary>
    public List<string>? Images { get; set; }

    public CarDto? Car { get; set; }
    public RealEstateDto? RealEstate { get; set; }

    public class CarDto
    {
        public int? BrandId { get; set; }
        public int? ModelId { get; set; }
        public string? Variant { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
        public FuelType Fuel { get; set; }
        public TransmissionType Transmission { get; set; }
        public BodyType Body { get; set; }
        public Drivetrain Drive { get; set; }
        public int? EngineCc { get; set; }
        public int? PowerHp { get; set; }
        public string? Color { get; set; }
        public List<CarOptions>? Options { get; set; }
    }

    public class RealEstateDto
    {
        public PropertyType PropertyType { get; set; }
        public TransactionType Transaction { get; set; }
        public int? Rooms { get; set; }
        public double SurfaceM2 { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public int? YearBuilt { get; set; }
        public HeatingType Heating { get; set; }
        public FurnishedState Furnished { get; set; }
        public List<PropertyAmenities>? Amenities { get; set; }
    }
}
