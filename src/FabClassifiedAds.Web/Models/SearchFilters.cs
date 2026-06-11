using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Models;

public enum SortOption { Newest = 0, PriceAsc = 1, PriceDesc = 2, MostViewed = 3, MileageAsc = 4, YearDesc = 5, SurfaceDesc = 6 }

public class SearchFilters
{
    // common
    public string? Q { get; set; }
    public string? Category { get; set; }            // slug
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? City { get; set; }
    public ItemCondition? Condition { get; set; }
    public SellerType? Seller { get; set; }
    public bool WithImagesOnly { get; set; }
    public bool NegotiableOnly { get; set; }
    public SortOption Sort { get; set; } = SortOption.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;

    // cars
    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public int? MileageMin { get; set; }
    public int? MileageMax { get; set; }
    public FuelType? Fuel { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyType? Body { get; set; }
    public Drivetrain? Drive { get; set; }
    public int? EngineCcMin { get; set; }
    public int? EngineCcMax { get; set; }
    public int? PowerMin { get; set; }
    public int? PowerMax { get; set; }
    public string? Color { get; set; }
    public int? EmissionStandard { get; set; }
    public bool AccidentFreeOnly { get; set; }
    public bool ServiceBookOnly { get; set; }
    public List<CarOptions> Options { get; set; } = [];

    // real estate
    public PropertyType? PropertyType { get; set; }
    public TransactionType? Transaction { get; set; }
    public int? RoomsMin { get; set; }
    public int? RoomsMax { get; set; }
    public double? SurfaceMin { get; set; }
    public double? SurfaceMax { get; set; }
    public int? FloorMin { get; set; }
    public int? FloorMax { get; set; }
    public int? YearBuiltMin { get; set; }
    public int? YearBuiltMax { get; set; }
    public HeatingType? Heating { get; set; }
    public FurnishedState? Furnished { get; set; }
    public List<PropertyAmenities> Amenities { get; set; } = [];

    public bool HasCarFilters =>
        BrandId.HasValue || ModelId.HasValue || YearMin.HasValue || YearMax.HasValue || MileageMin.HasValue ||
        MileageMax.HasValue || Fuel.HasValue || Transmission.HasValue || Body.HasValue || Drive.HasValue ||
        EngineCcMin.HasValue || EngineCcMax.HasValue || PowerMin.HasValue || PowerMax.HasValue ||
        !string.IsNullOrEmpty(Color) || EmissionStandard.HasValue || AccidentFreeOnly || ServiceBookOnly || Options.Count > 0;

    public bool HasRealEstateFilters =>
        PropertyType.HasValue || Transaction.HasValue || RoomsMin.HasValue || RoomsMax.HasValue ||
        SurfaceMin.HasValue || SurfaceMax.HasValue || FloorMin.HasValue || FloorMax.HasValue ||
        YearBuiltMin.HasValue || YearBuiltMax.HasValue || Heating.HasValue || Furnished.HasValue || Amenities.Count > 0;
}
