namespace FabClassifiedAds.Web.Models.Entities;

public class CarBrand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsPopular { get; set; }
    public List<CarModel> Models { get; set; } = [];
}

public class CarModel
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public CarBrand Brand { get; set; } = null!;
    public string Name { get; set; } = "";
}

public enum FuelType { Petrol = 0, Diesel = 1, Hybrid = 2, PlugInHybrid = 3, Electric = 4, Lpg = 5, Cng = 6, HydrogenFuelCell = 7 }
public enum TransmissionType { Manual = 0, Automatic = 1, SemiAutomatic = 2, Cvt = 3 }
public enum BodyType { Sedan = 0, Hatchback = 1, Estate = 2, Suv = 3, Coupe = 4, Convertible = 5, Pickup = 6, Minivan = 7, Van = 8, OffRoad = 9 }
public enum Drivetrain { FrontWheel = 0, RearWheel = 1, AllWheel = 2 }

[Flags]
public enum CarOptions : long
{
    None = 0,
    Abs = 1L << 0,
    Esp = 1L << 1,
    AirConditioning = 1L << 2,
    ClimateControl = 1L << 3,
    CruiseControl = 1L << 4,
    AdaptiveCruiseControl = 1L << 5,
    HeatedSeats = 1L << 6,
    VentilatedSeats = 1L << 7,
    LeatherInterior = 1L << 8,
    Sunroof = 1L << 9,
    PanoramicRoof = 1L << 10,
    Navigation = 1L << 11,
    AppleCarPlayAndroidAuto = 1L << 12,
    ParkingSensors = 1L << 13,
    RearCamera = 1L << 14,
    Camera360 = 1L << 15,
    KeylessEntry = 1L << 16,
    XenonLed = 1L << 17,
    MatrixHeadlights = 1L << 18,
    AlloyWheels = 1L << 19,
    TowBar = 1L << 20,
    LaneAssist = 1L << 21,
    BlindSpotMonitor = 1L << 22,
    HeadUpDisplay = 1L << 23,
    ElectricSeats = 1L << 24,
    MemorySeats = 1L << 25,
    HeatedSteeringWheel = 1L << 26,
    AmbientLighting = 1L << 27,
    PremiumSound = 1L << 28,
    WirelessCharging = 1L << 29,
    AutoParking = 1L << 30,
    NightVision = 1L << 31,
    AirSuspension = 1L << 32,
    SportPackage = 1L << 33,
    WinterTiresIncluded = 1L << 34,
    IsofixMounts = 1L << 35,
}

public class CarDetails
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public int? BrandId { get; set; }
    public CarBrand? Brand { get; set; }
    public int? ModelId { get; set; }
    public CarModel? Model { get; set; }
    public string? Variant { get; set; }            // e.g. "320d xDrive M Sport"

    public int Year { get; set; }
    public int Mileage { get; set; }                // km
    public FuelType Fuel { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyType Body { get; set; }
    public Drivetrain Drive { get; set; }

    public int? EngineCc { get; set; }              // engine displacement cm3
    public int? PowerHp { get; set; }
    public int? Doors { get; set; }
    public int? Seats { get; set; }
    public string? Color { get; set; }
    public bool Metallic { get; set; }

    public string? Vin { get; set; }
    public int? EmissionStandard { get; set; }      // Euro 3..7
    public int? Co2GKm { get; set; }
    public double? FuelConsumption { get; set; }    // l/100km or kWh/100km
    public int? BatteryKwh { get; set; }            // EVs
    public int? RangeKm { get; set; }               // EVs

    public int OwnersCount { get; set; } = 1;
    public bool AccidentFree { get; set; } = true;
    public bool ServiceBook { get; set; }
    public bool FirstRegistrationLocal { get; set; }
    public bool RightHandDrive { get; set; }
    public DateTime? InspectionValidUntil { get; set; }

    public CarOptions Options { get; set; } = CarOptions.None;
}
