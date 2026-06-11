namespace FabClassifiedAds.Web.Models.Entities;

public enum PropertyType { Apartment = 0, House = 1, Studio = 2, Penthouse = 3, Duplex = 4, Villa = 5, Land = 6, Commercial = 7, Office = 8, Garage = 9, Room = 10, Warehouse = 11, Building = 12 }
public enum TransactionType { Sale = 0, Rent = 1, RentShortTerm = 2 }
public enum HeatingType { None = 0, Central = 1, OwnGasBoiler = 2, Electric = 3, HeatPump = 4, Wood = 5, District = 6, UnderfloorHeating = 7 }
public enum EnergyClass { A = 0, B = 1, C = 2, D = 3, E = 4, F = 5, G = 6, Unknown = 7 }
public enum FurnishedState { Unfurnished = 0, PartiallyFurnished = 1, Furnished = 2, LuxuryFurnished = 3 }

[Flags]
public enum PropertyAmenities : long
{
    None = 0,
    Balcony = 1L << 0,
    Terrace = 1L << 1,
    Garden = 1L << 2,
    ParkingSpot = 1L << 3,
    Garage = 1L << 4,
    Elevator = 1L << 5,
    AirConditioning = 1L << 6,
    Basement = 1L << 7,
    StorageRoom = 1L << 8,
    SwimmingPool = 1L << 9,
    Sauna = 1L << 10,
    Gym = 1L << 11,
    Security24h = 1L << 12,
    VideoIntercom = 1L << 13,
    Alarm = 1L << 14,
    SmartHome = 1L << 15,
    SolarPanels = 1L << 16,
    EvCharger = 1L << 17,
    Fireplace = 1L << 18,
    PetsAllowed = 1L << 19,
    WheelchairAccess = 1L << 20,
    SeaView = 1L << 21,
    MountainView = 1L << 22,
    Dishwasher = 1L << 23,
    WashingMachine = 1L << 24,
    Internet = 1L << 25,
    CableTv = 1L << 26,
    NewBuilding = 1L << 27,
    Renovated = 1L << 28,
}

public class RealEstateDetails
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public PropertyType PropertyType { get; set; }
    public TransactionType Transaction { get; set; }

    public int? Rooms { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public double SurfaceM2 { get; set; }
    public double? LandSurfaceM2 { get; set; }
    public int? Floor { get; set; }                 // 0 = ground
    public int? TotalFloors { get; set; }
    public int? YearBuilt { get; set; }

    public HeatingType Heating { get; set; } = HeatingType.None;
    public EnergyClass EnergyClass { get; set; } = EnergyClass.Unknown;
    public FurnishedState Furnished { get; set; } = FurnishedState.Unfurnished;
    public PropertyAmenities Amenities { get; set; } = PropertyAmenities.None;

    public decimal? MonthlyMaintenanceCost { get; set; }
    public bool AvailableImmediately { get; set; } = true;
    public DateTime? AvailableFrom { get; set; }
}
