using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(CategorySeeder.Build());
            await db.SaveChangesAsync();
        }

        if (!await db.CarBrands.AnyAsync())
        {
            db.CarBrands.AddRange(CarDataSeeder.Build());
            await db.SaveChangesAsync();
        }

        if (!await db.Listings.AnyAsync())
            await SeedDemoListingsAsync(db, services.GetRequiredService<UserManager<ApplicationUser>>());
    }

    private static async Task SeedDemoListingsAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        var users = new List<ApplicationUser>();
        var demoUsers = new (string Name, string Email, string City, bool Business)[]
        {
            ("Alex Popescu", "alex@demo.fab", "Bucharest", false),
            ("Maria Ionescu", "maria@demo.fab", "Cluj-Napoca", false),
            ("AutoPremium SRL", "dealer@demo.fab", "Timisoara", true),
            ("Imobiliare Top", "estate@demo.fab", "Bucharest", true),
            ("Dan Munteanu", "dan@demo.fab", "Iasi", false),
        };
        var colors = new[] { "#6d5dfc", "#fc5d8d", "#28c76f", "#ff9f43", "#00cfe8" };
        for (var i = 0; i < demoUsers.Length; i++)
        {
            var (name, email, city, business) = demoUsers[i];
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                DisplayName = name, City = city, IsBusiness = business,
                AvatarColor = colors[i % colors.Length],
                RatingAverage = 4.2 + (i % 4) * 0.2, RatingCount = 12 + i * 9,
                CreatedAt = DateTime.UtcNow.AddMonths(-6 - i)
            };
            await userManager.CreateAsync(user, "Demo123!");
            users.Add(user);
        }

        var cats = await db.Categories.ToDictionaryAsync(c => c.Slug);
        var brands = await db.CarBrands.Include(b => b.Models).ToDictionaryAsync(b => b.Name);
        var rnd = new Random(42);
        var listings = new List<Listing>();

        Listing L(string title, string desc, decimal? price, string catSlug, ApplicationUser user,
                  string city, string region, bool featured = false, bool negotiable = true,
                  ItemCondition cond = ItemCondition.Used, string? imgSeed = null, int images = 3)
        {
            var l = new Listing
            {
                Title = title, Description = desc, Price = price, IsNegotiable = negotiable,
                Category = cats[catSlug], User = user, City = city, Region = region,
                Condition = cond, IsFeatured = featured,
                SellerType = user.IsBusiness ? SellerType.Business : SellerType.Private,
                ViewCount = rnd.Next(40, 4200),
                CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(0, 45)).AddHours(-rnd.Next(0, 23)),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            l.BumpedAt = l.CreatedAt;
            imgSeed ??= CategorySeeder.Slugify(title);
            for (var i = 0; i < images; i++)
                l.Images.Add(new ListingImage { Url = $"/media/ph/{imgSeed}-{i}.svg", SortOrder = i });
            listings.Add(l);
            return l;
        }

        CarDetails Car(string brand, string model, int year, int km, FuelType fuel, TransmissionType tr,
                       BodyType body, int cc, int hp, string color, CarOptions opts,
                       Drivetrain drive = Drivetrain.FrontWheel, int? batteryKwh = null, int? rangeKm = null)
        {
            var b = brands[brand];
            return new CarDetails
            {
                Brand = b, Model = b.Models.FirstOrDefault(m => m.Name == model),
                Year = year, Mileage = km, Fuel = fuel, Transmission = tr, Body = body, Drive = drive,
                EngineCc = cc == 0 ? null : cc, PowerHp = hp, Color = color, Metallic = true,
                Doors = body is BodyType.Coupe or BodyType.Convertible ? 2 : 5,
                Seats = 5, EmissionStandard = year >= 2015 ? 6 : 5,
                OwnersCount = rnd.Next(1, 3), AccidentFree = true, ServiceBook = true,
                BatteryKwh = batteryKwh, RangeKm = rangeKm,
                FuelConsumption = fuel == FuelType.Electric ? 16.5 : Math.Round(4.5 + rnd.NextDouble() * 5, 1),
                InspectionValidUntil = DateTime.UtcNow.AddMonths(rnd.Next(3, 22)),
                Options = opts
            };
        }

        var comfort = CarOptions.Abs | CarOptions.Esp | CarOptions.AirConditioning | CarOptions.CruiseControl |
                      CarOptions.ParkingSensors | CarOptions.AlloyWheels | CarOptions.IsofixMounts;
        var premium = comfort | CarOptions.ClimateControl | CarOptions.LeatherInterior | CarOptions.Navigation |
                      CarOptions.HeatedSeats | CarOptions.RearCamera | CarOptions.KeylessEntry | CarOptions.XenonLed |
                      CarOptions.AppleCarPlayAndroidAuto | CarOptions.AmbientLighting;
        var luxury = premium | CarOptions.PanoramicRoof | CarOptions.AdaptiveCruiseControl | CarOptions.Camera360 |
                     CarOptions.VentilatedSeats | CarOptions.HeadUpDisplay | CarOptions.MatrixHeadlights |
                     CarOptions.PremiumSound | CarOptions.MemorySeats | CarOptions.AirSuspension | CarOptions.AutoParking;

        // ---- Cars ----
        L("BMW 320d xDrive M Sport — Full LED, HUD", "BMW Seria 3 320d xDrive in M Sport trim. Full service history at BMW, second owner, no accidents. Head-up display, Harman Kardon, adaptive LED.", 28900, "cars", users[2], "Timisoara", "Timis", featured: true)
            .CarDetails = Car("BMW", "Seria 3", 2021, 78000, FuelType.Diesel, TransmissionType.Automatic, BodyType.Sedan, 1995, 190, "Mineral Grey", luxury, Drivetrain.AllWheel);
        L("Audi A4 Avant 2.0 TDI S-line", "Audi A4 Avant S-line, 2019, recently imported from Germany, no accidents, distronic, virtual cockpit.", 21500, "cars", users[2], "Timisoara", "Timis", featured: true)
            .CarDetails = Car("Audi", "A4", 2019, 124000, FuelType.Diesel, TransmissionType.Automatic, BodyType.Estate, 1968, 150, "Glacier White", premium);
        L("Tesla Model 3 Long Range Dual Motor", "Tesla Model 3 LR AWD, 2022, autopilot enhanced, white interior, 19\" sport wheels, warranty until 2030.", 31900, "cars", users[0], "Bucharest", "Bucharest", featured: true)
            .CarDetails = Car("Tesla", "Model 3", 2022, 41000, FuelType.Electric, TransmissionType.Automatic, BodyType.Sedan, 0, 498, "Pearl White", luxury, Drivetrain.AllWheel, 82, 580);
        L("Dacia Duster 1.5 dCi 4x4 Prestige", "Duster Prestige 4x4, first owner, bought new from Romania, full extras for this trim, winter package.", 13400, "cars", users[4], "Iasi", "Iasi")
            .CarDetails = Car("Dacia", "Duster", 2020, 67000, FuelType.Diesel, TransmissionType.Manual, BodyType.Suv, 1461, 115, "Atacama Orange", comfort | CarOptions.Navigation | CarOptions.RearCamera, Drivetrain.AllWheel);
        L("Volkswagen Golf 8 1.5 eTSI DSG R-Line", "Golf 8 R-Line, mild hybrid, IQ.Light matrix LED, travel assist, like new.", 23900, "cars", users[1], "Cluj-Napoca", "Cluj", featured: true)
            .CarDetails = Car("Volkswagen", "Golf", 2022, 29000, FuelType.Hybrid, TransmissionType.Automatic, BodyType.Hatchback, 1498, 150, "Lapiz Blue", premium | CarOptions.MatrixHeadlights | CarOptions.LaneAssist);
        L("Mercedes-Benz GLC 220d 4Matic AMG Line", "GLC AMG Line, Burmester audio, 360 camera, panoramic roof, airmatic. Trade-in possible.", 38500, "cars", users[2], "Timisoara", "Timis", featured: true)
            .CarDetails = Car("Mercedes-Benz", "GLC", 2021, 89000, FuelType.Diesel, TransmissionType.Automatic, BodyType.Suv, 1950, 194, "Obsidian Black", luxury, Drivetrain.AllWheel);
        L("Skoda Octavia Combi 2.0 TDI DSG", "Octavia 4 Combi Style, ACC, Canton sound, electric tailgate, ideal family car.", 19900, "cars", users[0], "Bucharest", "Bucharest")
            .CarDetails = Car("Skoda", "Octavia", 2021, 98000, FuelType.Diesel, TransmissionType.Automatic, BodyType.Estate, 1968, 150, "Race Blue", premium | CarOptions.AdaptiveCruiseControl);
        L("Ford Mustang GT 5.0 V8 — Performance Pack", "Mustang GT V8 manual, performance package, Recaro seats, active exhaust, US import fully registered.", 42000, "cars", users[4], "Iasi", "Iasi", featured: true)
            .CarDetails = Car("Ford", "Mustang", 2019, 36000, FuelType.Petrol, TransmissionType.Manual, BodyType.Coupe, 5038, 450, "Race Red", premium | CarOptions.SportPackage, Drivetrain.RearWheel);
        L("Renault Clio 1.0 TCe — first owner", "Clio 5, bought new, garage kept, perfect city car, low consumption.", 11200, "cars", users[1], "Cluj-Napoca", "Cluj")
            .CarDetails = Car("Renault", "Clio", 2021, 34000, FuelType.Petrol, TransmissionType.Manual, BodyType.Hatchback, 999, 90, "Valencia Orange", comfort | CarOptions.AppleCarPlayAndroidAuto);
        L("Hyundai Tucson 1.6 T-GDI Hybrid Luxury", "Tucson Hybrid Luxury, krell audio, vented seats, remote parking, full warranty until 2027.", 27800, "cars", users[2], "Timisoara", "Timis")
            .CarDetails = Car("Hyundai", "Tucson", 2022, 45000, FuelType.Hybrid, TransmissionType.Automatic, BodyType.Suv, 1598, 230, "Amazon Grey", luxury);
        L("Porsche Macan S — approved warranty", "Macan S, PASM, BOSE, panoramic roof, sport chrono, Porsche Approved warranty.", 56900, "cars", users[2], "Timisoara", "Timis", featured: true)
            .CarDetails = Car("Porsche", "Macan", 2020, 61000, FuelType.Petrol, TransmissionType.Automatic, BodyType.Suv, 2995, 354, "Carrara White", luxury, Drivetrain.AllWheel);
        L("Toyota Corolla 1.8 Hybrid — taxi-free", "Corolla hybrid sedan, never used as taxi, full Toyota history, 5.: real consumption 4.1l/100km.", 18700, "cars", users[0], "Bucharest", "Bucharest")
            .CarDetails = Car("Toyota", "Corolla", 2020, 87000, FuelType.Hybrid, TransmissionType.Cvt, BodyType.Sedan, 1798, 122, "Silver Metallic", premium);

        // ---- Real estate ----
        RealEstateDetails Re(PropertyType pt, TransactionType tx, int? rooms, double m2, int? floor, int? totalFloors,
                             int? yearBuilt, HeatingType heat, FurnishedState furn, PropertyAmenities am,
                             int? beds = null, int? baths = null, double? land = null)
            => new()
            {
                PropertyType = pt, Transaction = tx, Rooms = rooms, SurfaceM2 = m2, Floor = floor,
                TotalFloors = totalFloors, YearBuilt = yearBuilt, Heating = heat, Furnished = furn, Amenities = am,
                Bedrooms = beds ?? (rooms.HasValue ? Math.Max(1, rooms.Value - 1) : null), Bathrooms = baths ?? 1,
                LandSurfaceM2 = land, EnergyClass = (EnergyClass)rnd.Next(0, 4)
            };

        L("Modern 3-room apartment, Pipera — first rental", "Bright 3-room apartment in a 2022 building near the metro. Underground parking, storage room, smart home system included.", 1100, "apartments-for-rent", users[3], "Bucharest", "Bucharest", featured: true)
            .RealEstateDetails = Re(PropertyType.Apartment, TransactionType.Rent, 3, 78, 4, 8, 2022, HeatingType.OwnGasBoiler, FurnishedState.LuxuryFurnished,
                PropertyAmenities.Balcony | PropertyAmenities.ParkingSpot | PropertyAmenities.Elevator | PropertyAmenities.AirConditioning | PropertyAmenities.SmartHome | PropertyAmenities.StorageRoom | PropertyAmenities.VideoIntercom | PropertyAmenities.NewBuilding, beds: 2, baths: 2);
        L("2-room apartment for sale, Marasti, renovated 2024", "Fully renovated 2-room apartment, new appliances, AC in both rooms, 5 min to Iulius Mall.", 132000, "apartments-for-sale", users[1], "Cluj-Napoca", "Cluj", featured: true)
            .RealEstateDetails = Re(PropertyType.Apartment, TransactionType.Sale, 2, 54, 2, 4, 1984, HeatingType.Central, FurnishedState.Furnished,
                PropertyAmenities.Balcony | PropertyAmenities.AirConditioning | PropertyAmenities.Renovated | PropertyAmenities.CableTv | PropertyAmenities.Internet);
        L("Villa with pool, Corbeanca — 5 rooms", "Premium villa on 800m2 plot: heated pool, sauna, triple garage, heat pump with underfloor heating, solar panels.", 459000, "houses-for-sale", users[3], "Corbeanca", "Ilfov", featured: true)
            .RealEstateDetails = Re(PropertyType.Villa, TransactionType.Sale, 5, 240, null, 2, 2019, HeatingType.HeatPump, FurnishedState.PartiallyFurnished,
                PropertyAmenities.Garden | PropertyAmenities.SwimmingPool | PropertyAmenities.Sauna | PropertyAmenities.Garage | PropertyAmenities.SolarPanels | PropertyAmenities.SmartHome | PropertyAmenities.Alarm | PropertyAmenities.Terrace | PropertyAmenities.Fireplace | PropertyAmenities.EvCharger, beds: 4, baths: 3, land: 800);
        L("Studio for rent, ultracentral Iasi", "Cozy studio in the old center, perfect for a student or young professional. All utilities included in rent.", 380, "apartments-for-rent", users[4], "Iasi", "Iasi")
            .RealEstateDetails = Re(PropertyType.Studio, TransactionType.Rent, 1, 32, 3, 5, 2010, HeatingType.OwnGasBoiler, FurnishedState.Furnished,
                PropertyAmenities.AirConditioning | PropertyAmenities.Internet | PropertyAmenities.WashingMachine | PropertyAmenities.Dishwasher | PropertyAmenities.PetsAllowed);
        L("Penthouse with panoramic terrace, Floreasca", "Spectacular 4-room penthouse, 120m2 terrace with city view, 2 parking spots, concierge building.", 549000, "apartments-for-sale", users[3], "Bucharest", "Bucharest", featured: true)
            .RealEstateDetails = Re(PropertyType.Penthouse, TransactionType.Sale, 4, 168, 11, 12, 2021, HeatingType.HeatPump, FurnishedState.LuxuryFurnished,
                PropertyAmenities.Terrace | PropertyAmenities.ParkingSpot | PropertyAmenities.Elevator | PropertyAmenities.AirConditioning | PropertyAmenities.Security24h | PropertyAmenities.Gym | PropertyAmenities.SmartHome | PropertyAmenities.NewBuilding, beds: 3, baths: 3);
        L("Buildable land 1200m2, Feleacu — utilities at gate", "Intravilan plot, 20m street front, all utilities, PUZ approved for P+2, panoramic view of Cluj.", 96000, "land-and-plots", users[1], "Feleacu", "Cluj")
            .RealEstateDetails = Re(PropertyType.Land, TransactionType.Sale, null, 1200, null, null, null, HeatingType.None, FurnishedState.Unfurnished, PropertyAmenities.None, land: 1200);
        L("Office space 220m2, class A building", "Open-space office, fitted out, raised floor, 6 parking spots, BREEAM certified building near metro.", 3300, "offices", users[3], "Bucharest", "Bucharest")
            .RealEstateDetails = Re(PropertyType.Office, TransactionType.Rent, null, 220, 6, 14, 2018, HeatingType.District, FurnishedState.PartiallyFurnished,
                PropertyAmenities.Elevator | PropertyAmenities.AirConditioning | PropertyAmenities.ParkingSpot | PropertyAmenities.Security24h);
        L("House for rent, Gruia — pet friendly", "4-room house with yard, garage, recently renovated, pets welcome, long-term only.", 950, "houses-for-rent", users[1], "Cluj-Napoca", "Cluj")
            .RealEstateDetails = Re(PropertyType.House, TransactionType.Rent, 4, 130, null, 1, 1996, HeatingType.OwnGasBoiler, FurnishedState.PartiallyFurnished,
                PropertyAmenities.Garden | PropertyAmenities.Garage | PropertyAmenities.PetsAllowed | PropertyAmenities.Renovated | PropertyAmenities.Fireplace, land: 350);

        // ---- Electronics & others ----
        L("iPhone 15 Pro Max 256GB Natural Titanium", "Like new, 99% battery health, full box, Apple warranty until November. No scratches.", 1050, "iphone", users[0], "Bucharest", "Bucharest", featured: true, cond: ItemCondition.Used);
        L("MacBook Pro 14 M3 Pro 18GB/512GB", "Bought 6 months ago, 14 charge cycles, AppleCare+ until 2027, comes with original box and receipt.", 1850, "laptops", users[1], "Cluj-Napoca", "Cluj", featured: true);
        L("PlayStation 5 Slim + 2 controllers + 5 games", "PS5 Slim disc edition, extra DualSense, Spider-Man 2, God of War Ragnarok, FC25, Horizon, GT7.", 480, "playstation", users[4], "Iasi", "Iasi");
        L("Samsung Neo QLED 65\" QN90C", "Stunning 4K 144Hz TV, perfect for gaming and movies, wall mount included, 2 years warranty left.", 1100, "televisions", users[0], "Bucharest", "Bucharest");
        L("Canon EOS R6 Mark II + RF 24-105 f/4", "Professional mirrorless kit, 12k shutter count, 3 batteries, 2x 128GB cards, Peak Design strap.", 2750, "dslr-and-mirrorless-cameras", users[1], "Cluj-Napoca", "Cluj");
        L("DJI Mavic 3 Pro Fly More Combo", "Complete combo: 3 batteries, ND filters, 1TB microSD. A2 license not needed for C1 class.", 1980, "drones", users[0], "Bucharest", "Bucharest");
        L("Gaming PC — RTX 4080, Ryzen 7 7800X3D", "Custom build: 32GB DDR5 6000, 2TB NVMe Gen4, Lian Li O11, 360mm AIO. Receipts for all parts.", 2300, "gaming-pcs", users[4], "Iasi", "Iasi", featured: true);
        L("Bosch Serie 8 washing machine 10kg", "i-DOS automatic dosing, A class, 1600rpm, perfect condition, moving abroad sale.", 520, "washing-machines", users[1], "Cluj-Napoca", "Cluj");

        // ---- Home, fashion, sports, pets, jobs, services ----
        L("Corner sofa with sleeping function, velvet green", "Beautiful emerald velvet corner sofa, 280x180cm, storage compartment, 1 year old, no stains.", 650, "sofas-and-armchairs", users[0], "Bucharest", "Bucharest");
        L("Oak dining table + 6 chairs, solid wood", "Handmade solid oak table 200x100cm with 6 upholstered chairs. Can deliver in town.", 890, "tables-and-chairs", users[4], "Iasi", "Iasi");
        L("Rolex Datejust 41 — full set 2022", "Datejust 41 blue dial, jubilee bracelet, box and papers, purchased from AD in 2022, mint condition.", 11500, "luxury-watches", users[0], "Bucharest", "Bucharest", featured: true);
        L("Canyon Spectral 29 CF8 — carbon enduro", "Carbon frame size L, Fox 36 Performance Elite, XT 12sp, new brake pads, serviced this spring.", 2400, "mountain-bikes", users[1], "Cluj-Napoca", "Cluj");
        L("Technogym treadmill — commercial grade", "MyRun treadmill, barely used, perfect for apartments (quiet), app connectivity.", 1700, "treadmills-and-cardio", users[3], "Bucharest", "Bucharest");
        L("Fender Player Stratocaster + Marshall amp", "Player Strat sunburst, made in Mexico, with Marshall MG30 amp, cable and gig bag.", 720, "guitars-and-basses", users[4], "Iasi", "Iasi");
        L("Golden Retriever puppies with pedigree", "3 males and 2 females, vaccinated, dewormed, microchipped, FCI pedigree, parents can be seen.", 600, "dogs", users[1], "Cluj-Napoca", "Cluj");
        L("Complete aquarium 240l Juwel Rio", "Juwel Rio 240 with cabinet, LED lighting, Bioflow filter, heater and decorations included.", 350, "fish-and-aquariums", users[0], "Bucharest", "Bucharest");
        L("Senior .NET Developer — hybrid, Bucharest", "Product company building fintech solutions. .NET 9+, Azure, microservices. 2 days office/week. Salary range 5500-7500 EUR net.", null, "it-and-software", users[3], "Bucharest", "Bucharest", negotiable: false);
        L("Delivery drivers wanted — flexible schedule", "Courier company hiring B-license drivers. Choose your own hours, weekly pay, fuel card provided.", null, "transport-and-logistics", users[2], "Timisoara", "Timis", negotiable: false);
        L("Complete apartment renovation services", "Team of professionals: painting, flooring, electrical, plumbing, bathroom refits. Free quote, contract and warranty.", null, "construction-and-renovation", users[4], "Iasi", "Iasi", negotiable: false);
        L("Wedding & event photography", "10 years experience, two photographers, drone footage, online gallery in 7 days. Packages from 800 EUR.", 800, "photo-and-video-services", users[0], "Bucharest", "Bucharest");
        L("Michelin Pilot Sport 5 — 225/45 R17, set of 4", "Brand new, never mounted, DOT 2025. Bought wrong size, selling at loss.", 420, "summer-tires", users[2], "Timisoara", "Timis", cond: ItemCondition.New);
        L("Thule Motion XT XL roof box + bars", "920 model in glossy black, used for two trips, includes Thule WingBar Evo bars for BMW X3.", 540, "car-care-and-tuning", users[1], "Cluj-Napoca", "Cluj");
        L("Vespa GTS 300 Super — 2023, like new", "GTS 300 hpe, 1900 km, still under warranty, two helmets and top case included.", 5600, "scooters-and-mopeds", users[0], "Bucharest", "Bucharest");
        L("Yamaha MT-07 2022 — A2 compatible", "MT-07 with Akrapovic exhaust, tail tidy, 9000 km, first owner, never dropped.", 6400, "naked-bikes", users[4], "Iasi", "Iasi");

        db.Listings.AddRange(listings);
        await db.SaveChangesAsync();
    }
}
