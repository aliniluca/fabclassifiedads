using FabClassifiedAds.Web.Models.Entities;

namespace FabClassifiedAds.Web.Data;

public static class CategorySeeder
{
    private record Cat(string Name, string Icon, CategoryKind Kind = CategoryKind.Generic, params Cat[] Children)
    {
        public Cat(string name, string icon, params Cat[] children)
            : this(name, icon, CategoryKind.Generic, children) { }
    }

    private static readonly Cat[] Tree =
    [
        new Cat("Auto, Moto & Boats", "🚗", CategoryKind.Generic,
            new Cat("Cars", "🚙", CategoryKind.Cars),
            new Cat("Motorcycles", "🏍️", CategoryKind.Motorcycles,
                new Cat("Sport bikes", "🏍️"), new Cat("Cruisers & Choppers", "🛵"), new Cat("Touring", "🧳"),
                new Cat("Enduro & Cross", "⛰️"), new Cat("Scooters & Mopeds", "🛵"), new Cat("ATV & Quad", "🚜"), new Cat("Naked bikes", "🏍️")),
            new Cat("Trucks & Commercial", "🚚",
                new Cat("Vans up to 3.5t", "🚐"), new Cat("Trucks over 3.5t", "🚛"), new Cat("Tractor units", "🚛"),
                new Cat("Buses & Minibuses", "🚌"), new Cat("Trailers & Semi-trailers", "🚛")),
            new Cat("Boats & Watercraft", "⛵",
                new Cat("Motor boats", "🚤"), new Cat("Sailing boats", "⛵"), new Cat("Jet skis", "🌊"), new Cat("Kayaks & Canoes", "🛶")),
            new Cat("Agricultural vehicles", "🚜",
                new Cat("Tractors", "🚜"), new Cat("Combines", "🌾"), new Cat("Agricultural equipment", "⚙️")),
            new Cat("Construction machinery", "🏗️",
                new Cat("Excavators", "🏗️"), new Cat("Loaders", "🏗️"), new Cat("Forklifts", "📦")),
            new Cat("Auto parts & Accessories", "🔧",
                new Cat("Engine & Drivetrain parts", "⚙️"), new Cat("Body parts", "🚗"), new Cat("Brakes & Suspension", "🛞"),
                new Cat("Electrics & Electronics", "🔌"), new Cat("Interior parts", "💺"), new Cat("Car audio & GPS", "🔊"),
                new Cat("Car care & Tuning", "✨"), new Cat("Oils & Fluids", "🛢️")),
            new Cat("Tires & Wheels", "🛞",
                new Cat("Summer tires", "☀️"), new Cat("Winter tires", "❄️"), new Cat("All-season tires", "🌦️"),
                new Cat("Alloy wheels", "🛞"), new Cat("Steel wheels", "🛞"), new Cat("Complete wheel sets", "🛞")),
            new Cat("Campers & Caravans", "🚐")),

        new Cat("Real Estate", "🏠", CategoryKind.Generic,
            new Cat("Apartments for sale", "🏢", CategoryKind.RealEstate),
            new Cat("Apartments for rent", "🔑", CategoryKind.RealEstate),
            new Cat("Houses for sale", "🏡", CategoryKind.RealEstate),
            new Cat("Houses for rent", "🏠", CategoryKind.RealEstate),
            new Cat("Land & Plots", "🌍", CategoryKind.RealEstate),
            new Cat("Commercial spaces", "🏪", CategoryKind.RealEstate),
            new Cat("Offices", "🏢", CategoryKind.RealEstate),
            new Cat("Warehouses & Industrial", "🏭", CategoryKind.RealEstate),
            new Cat("Garages & Parking", "🅿️", CategoryKind.RealEstate),
            new Cat("Rooms for rent / Flatmates", "🛏️", CategoryKind.RealEstate),
            new Cat("Vacation rentals", "🏖️", CategoryKind.RealEstate),
            new Cat("New developments", "🏗️", CategoryKind.RealEstate)),

        new Cat("Electronics & Appliances", "📱", CategoryKind.Generic,
            new Cat("Mobile phones", "📱", CategoryKind.Electronics,
                new Cat("iPhone", "🍎"), new Cat("Samsung", "📱"), new Cat("Xiaomi", "📱"), new Cat("Google Pixel", "📱"),
                new Cat("Huawei", "📱"), new Cat("OnePlus", "📱"), new Cat("Other phones", "📱"),
                new Cat("Phone accessories", "🔌"), new Cat("Smartwatches & Wearables", "⌚")),
            new Cat("Computers & Laptops", "💻", CategoryKind.Electronics,
                new Cat("Laptops", "💻"), new Cat("Desktop PCs", "🖥️"), new Cat("Gaming PCs", "🎮"),
                new Cat("Monitors", "🖥️"), new Cat("Components (CPU, GPU, RAM)", "🧩"),
                new Cat("Peripherals", "⌨️"), new Cat("Printers & Scanners", "🖨️"), new Cat("Networking", "📡")),
            new Cat("Tablets & E-readers", "📲"),
            new Cat("TV & Home cinema", "📺",
                new Cat("Televisions", "📺"), new Cat("Projectors", "📽️"), new Cat("Soundbars & Speakers", "🔊"), new Cat("Streaming devices", "📡")),
            new Cat("Audio & Hi-Fi", "🎧",
                new Cat("Headphones", "🎧"), new Cat("Speakers", "🔊"), new Cat("Amplifiers & Receivers", "🎚️"), new Cat("Turntables & Vinyl", "💿")),
            new Cat("Photo & Video", "📷",
                new Cat("DSLR & Mirrorless cameras", "📷"), new Cat("Lenses", "🔭"), new Cat("Drones", "🛸"),
                new Cat("Action cameras", "🎥"), new Cat("Studio & Lighting", "💡")),
            new Cat("Gaming & Consoles", "🎮",
                new Cat("PlayStation", "🎮"), new Cat("Xbox", "🎮"), new Cat("Nintendo", "🕹️"),
                new Cat("Games", "💾"), new Cat("VR headsets", "🥽"), new Cat("Gaming accessories", "🎮")),
            new Cat("Large appliances", "🧊",
                new Cat("Refrigerators", "🧊"), new Cat("Washing machines", "🌀"), new Cat("Dishwashers", "🍽️"),
                new Cat("Ovens & Stoves", "🔥"), new Cat("Dryers", "🌀")),
            new Cat("Small appliances", "☕",
                new Cat("Coffee machines", "☕"), new Cat("Vacuum cleaners", "🧹"), new Cat("Kitchen appliances", "🍳"),
                new Cat("Air purifiers & Fans", "💨"), new Cat("Personal care", "💈"))),

        new Cat("Home & Garden", "🛋️", CategoryKind.Generic,
            new Cat("Furniture", "🛋️",
                new Cat("Sofas & Armchairs", "🛋️"), new Cat("Beds & Mattresses", "🛏️"), new Cat("Tables & Chairs", "🪑"),
                new Cat("Wardrobes & Storage", "🚪"), new Cat("Office furniture", "🖥️")),
            new Cat("Home decor", "🖼️",
                new Cat("Lighting", "💡"), new Cat("Rugs & Carpets", "🧶"), new Cat("Curtains & Textiles", "🪟"),
                new Cat("Mirrors & Wall art", "🖼️"), new Cat("Candles & Scents", "🕯️")),
            new Cat("Kitchen & Dining", "🍽️"),
            new Cat("Garden & Terrace", "🌳",
                new Cat("Garden furniture", "🪑"), new Cat("BBQ & Grills", "🍖"), new Cat("Plants & Seeds", "🌱"),
                new Cat("Lawn mowers & Garden tools", "🌿"), new Cat("Pools & Accessories", "🏊")),
            new Cat("Tools & DIY", "🔨",
                new Cat("Power tools", "🪛"), new Cat("Hand tools", "🔨"), new Cat("Welding equipment", "⚡"), new Cat("Ladders & Scaffolding", "🪜")),
            new Cat("Construction materials", "🧱",
                new Cat("Bricks, Cement & Aggregates", "🧱"), new Cat("Doors & Windows", "🚪"),
                new Cat("Flooring & Tiles", "🟫"), new Cat("Plumbing & Heating", "🚿"), new Cat("Electrical supplies", "🔌"))),

        new Cat("Fashion & Beauty", "👗", CategoryKind.Generic,
            new Cat("Women's clothing", "👗", CategoryKind.Fashion,
                new Cat("Dresses", "👗"), new Cat("Tops & Shirts", "👚"), new Cat("Jeans & Trousers", "👖"),
                new Cat("Jackets & Coats", "🧥"), new Cat("Activewear", "🏃‍♀️")),
            new Cat("Men's clothing", "👔", CategoryKind.Fashion,
                new Cat("Shirts & T-shirts", "👕"), new Cat("Jeans & Trousers", "👖"),
                new Cat("Jackets & Coats", "🧥"), new Cat("Suits", "🤵")),
            new Cat("Shoes", "👟",
                new Cat("Women's shoes", "👠"), new Cat("Men's shoes", "👞"), new Cat("Sneakers", "👟"), new Cat("Boots", "🥾")),
            new Cat("Bags & Accessories", "👜",
                new Cat("Handbags", "👜"), new Cat("Backpacks", "🎒"), new Cat("Wallets & Belts", "👝"), new Cat("Sunglasses", "🕶️")),
            new Cat("Watches & Jewelry", "⌚",
                new Cat("Luxury watches", "⌚"), new Cat("Fashion watches", "⌚"), new Cat("Rings & Necklaces", "💍"), new Cat("Gold & Silver", "🥇")),
            new Cat("Beauty & Health", "💄",
                new Cat("Perfumes", "🌸"), new Cat("Makeup", "💄"), new Cat("Skincare", "🧴"), new Cat("Hair styling", "💇"))),

        new Cat("Mom & Kids", "🧸", CategoryKind.Generic,
            new Cat("Strollers & Car seats", "👶",
                new Cat("Strollers", "👶"), new Cat("Car seats", "🚗"), new Cat("Carriers & Slings", "🤱")),
            new Cat("Kids' clothing & Shoes", "👶"),
            new Cat("Toys & Games", "🧸",
                new Cat("LEGO & Building sets", "🧱"), new Cat("Dolls & Plush", "🧸"), new Cat("Outdoor toys", "🛝"), new Cat("Educational toys", "🎓")),
            new Cat("Kids' furniture", "🛏️"),
            new Cat("Baby essentials", "🍼"),
            new Cat("School supplies", "🎒")),

        new Cat("Sports, Hobby & Leisure", "⚽", CategoryKind.Generic,
            new Cat("Bicycles", "🚲",
                new Cat("Road bikes", "🚴"), new Cat("Mountain bikes", "⛰️"), new Cat("E-bikes", "🔋"),
                new Cat("City & Trekking bikes", "🚲"), new Cat("Kids' bikes", "🚲"), new Cat("Bike parts & accessories", "🔧")),
            new Cat("E-scooters & Mobility", "🛴"),
            new Cat("Fitness & Gym", "🏋️",
                new Cat("Treadmills & Cardio", "🏃"), new Cat("Weights & Dumbbells", "🏋️"), new Cat("Home gyms", "💪"), new Cat("Yoga & Pilates", "🧘")),
            new Cat("Winter sports", "⛷️",
                new Cat("Skis", "🎿"), new Cat("Snowboards", "🏂"), new Cat("Boots & Bindings", "🥾"), new Cat("Ski apparel", "🧥")),
            new Cat("Water sports", "🏄",
                new Cat("Surf & SUP", "🏄"), new Cat("Diving gear", "🤿"), new Cat("Swimming", "🏊")),
            new Cat("Fishing & Hunting", "🎣",
                new Cat("Rods & Reels", "🎣"), new Cat("Lures & Bait", "🪱"), new Cat("Hunting gear", "🏹")),
            new Cat("Camping & Outdoor", "🏕️",
                new Cat("Tents", "⛺"), new Cat("Sleeping bags & Mats", "🛌"), new Cat("Hiking gear", "🥾")),
            new Cat("Team sports", "⚽",
                new Cat("Football", "⚽"), new Cat("Basketball", "🏀"), new Cat("Tennis & Padel", "🎾"), new Cat("Hockey", "🏒")),
            new Cat("Musical instruments", "🎸",
                new Cat("Guitars & Basses", "🎸"), new Cat("Keyboards & Pianos", "🎹"), new Cat("Drums & Percussion", "🥁"),
                new Cat("DJ & Studio equipment", "🎛️"), new Cat("Wind & String instruments", "🎻")),
            new Cat("Books, Movies & Music", "📚",
                new Cat("Books", "📖"), new Cat("Comics & Manga", "💬"), new Cat("Vinyl & CDs", "💿"), new Cat("Movies & Series", "🎬")),
            new Cat("Collectibles & Art", "🖼️",
                new Cat("Coins & Banknotes", "🪙"), new Cat("Stamps", "📮"), new Cat("Antiques", "🏺"),
                new Cat("Trading cards", "🃏"), new Cat("Paintings & Art", "🎨")),
            new Cat("Board games & Puzzles", "🎲"),
            new Cat("Tickets & Events", "🎟️")),

        new Cat("Animals", "🐾", CategoryKind.Generic,
            new Cat("Dogs", "🐕"),
            new Cat("Cats", "🐈"),
            new Cat("Birds", "🦜"),
            new Cat("Fish & Aquariums", "🐠"),
            new Cat("Small animals", "🐹"),
            new Cat("Farm animals", "🐄"),
            new Cat("Pet food & Accessories", "🦴"),
            new Cat("Pet services", "✂️")),

        new Cat("Agriculture & Industry", "🌾", CategoryKind.Generic,
            new Cat("Cereals & Crops", "🌾"),
            new Cat("Industrial equipment", "🏭"),
            new Cat("Beekeeping", "🐝"),
            new Cat("Firewood & Fuel", "🪵"),
            new Cat("Business for sale", "💼")),

        new Cat("Jobs", "💼", CategoryKind.Generic,
            new Cat("IT & Software", "💻", CategoryKind.Jobs),
            new Cat("Engineering", "⚙️", CategoryKind.Jobs),
            new Cat("Construction & Trades", "👷", CategoryKind.Jobs),
            new Cat("Transport & Logistics", "🚚", CategoryKind.Jobs),
            new Cat("Retail & Sales", "🛒", CategoryKind.Jobs),
            new Cat("Hospitality & Tourism", "🏨", CategoryKind.Jobs),
            new Cat("Healthcare", "⚕️", CategoryKind.Jobs),
            new Cat("Education", "🎓", CategoryKind.Jobs),
            new Cat("Finance & Accounting", "📊", CategoryKind.Jobs),
            new Cat("Marketing & PR", "📣", CategoryKind.Jobs),
            new Cat("Customer support", "🎧", CategoryKind.Jobs),
            new Cat("Cleaning & Housekeeping", "🧹", CategoryKind.Jobs),
            new Cat("Beauty & Wellness jobs", "💇", CategoryKind.Jobs),
            new Cat("Agriculture jobs", "🌾", CategoryKind.Jobs),
            new Cat("Work abroad", "✈️", CategoryKind.Jobs)),

        new Cat("Services", "🛠️", CategoryKind.Generic,
            new Cat("Construction & Renovation", "🏗️"),
            new Cat("Electrician & Plumber", "🔌"),
            new Cat("Moving & Transport", "📦"),
            new Cat("Auto services", "🔧"),
            new Cat("Cleaning services", "🧽"),
            new Cat("IT & Web services", "💻"),
            new Cat("Photo & Video services", "📸"),
            new Cat("Events & Catering", "🎉"),
            new Cat("Tutoring & Courses", "📚"),
            new Cat("Beauty services", "💅"),
            new Cat("Legal & Accounting", "⚖️"),
            new Cat("Repair services", "🛠️"))
    ];

    public static List<Category> Build()
    {
        var result = new List<Category>();
        var usedSlugs = new HashSet<string>();
        var sort = 0;
        foreach (var root in Tree)
            result.Add(BuildNode(root, null, ref sort, usedSlugs));
        return result;
    }

    private static Category BuildNode(Cat cat, Category? parent, ref int sort, HashSet<string> usedSlugs)
    {
        var baseSlug = Slugify(cat.Name);
        var slug = baseSlug;
        var i = 2;
        while (!usedSlugs.Add(slug)) slug = $"{baseSlug}-{i++}";

        var entity = new Category
        {
            Name = cat.Name,
            Slug = slug,
            Icon = cat.Icon,
            Kind = cat.Kind,
            Parent = parent,
            SortOrder = sort++
        };
        foreach (var child in cat.Children)
            entity.Children.Add(BuildNode(child, entity, ref sort, usedSlugs));
        return entity;
    }

    public static string Slugify(string name)
    {
        var chars = name.ToLowerInvariant()
            .Replace("&", "and")
            .Select(c => char.IsLetterOrDigit(c) ? c : '-');
        var slug = new string(chars.ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
