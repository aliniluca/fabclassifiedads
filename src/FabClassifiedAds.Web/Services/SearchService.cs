using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

public class SearchResult
{
    public List<Listing> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class SearchService(AppDbContext db)
{
    public async Task<SearchResult> SearchAsync(SearchFilters f)
    {
        var query = db.Listings.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .Include(l => l.Category)
            .Include(l => l.User)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Brand)
            .Include(l => l.CarDetails!).ThenInclude(c => c.Model)
            .Include(l => l.RealEstateDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Category))
        {
            var catIds = await GetCategoryWithDescendantsAsync(f.Category);
            if (catIds.Count > 0)
                query = query.Where(l => catIds.Contains(l.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var q = f.Q.Trim();
            query = query.Where(l => EF.Functions.Like(l.Title, $"%{q}%") || EF.Functions.Like(l.Description, $"%{q}%"));
        }

        if (f.PriceMin.HasValue) query = query.Where(l => l.Price >= f.PriceMin);
        if (f.PriceMax.HasValue) query = query.Where(l => l.Price <= f.PriceMax);
        if (!string.IsNullOrWhiteSpace(f.City)) query = query.Where(l => EF.Functions.Like(l.City, $"%{f.City}%") || EF.Functions.Like(l.Region, $"%{f.City}%"));
        if (f.Condition.HasValue) query = query.Where(l => l.Condition == f.Condition);
        if (f.Seller.HasValue) query = query.Where(l => l.SellerType == f.Seller);
        if (f.WithImagesOnly) query = query.Where(l => l.Images.Any());
        if (f.NegotiableOnly) query = query.Where(l => l.IsNegotiable);

        query = ApplyCarFilters(query, f);
        query = ApplyRealEstateFilters(query, f);

        var total = await query.CountAsync();

        query = f.Sort switch
        {
            SortOption.PriceAsc => query.OrderBy(l => l.Price == null).ThenBy(l => l.Price),
            SortOption.PriceDesc => query.OrderByDescending(l => l.Price),
            SortOption.MostViewed => query.OrderByDescending(l => l.ViewCount),
            SortOption.MileageAsc => query.OrderBy(l => l.CarDetails == null).ThenBy(l => l.CarDetails!.Mileage),
            SortOption.YearDesc => query.OrderByDescending(l => l.CarDetails != null ? l.CarDetails.Year : 0),
            SortOption.SurfaceDesc => query.OrderByDescending(l => l.RealEstateDetails != null ? l.RealEstateDetails.SurfaceM2 : 0),
            _ => query.OrderByDescending(l => l.IsFeatured).ThenByDescending(l => l.BumpedAt),
        };

        var page = Math.Max(1, f.Page);
        var items = await query.Skip((page - 1) * f.PageSize).Take(f.PageSize).ToListAsync();

        return new SearchResult { Items = items, TotalCount = total, Page = page, PageSize = f.PageSize };
    }

    private static IQueryable<Listing> ApplyCarFilters(IQueryable<Listing> query, SearchFilters f)
    {
        if (!f.HasCarFilters) return query;

        query = query.Where(l => l.CarDetails != null);
        if (f.BrandId.HasValue) query = query.Where(l => l.CarDetails!.BrandId == f.BrandId);
        if (f.ModelId.HasValue) query = query.Where(l => l.CarDetails!.ModelId == f.ModelId);
        if (f.YearMin.HasValue) query = query.Where(l => l.CarDetails!.Year >= f.YearMin);
        if (f.YearMax.HasValue) query = query.Where(l => l.CarDetails!.Year <= f.YearMax);
        if (f.MileageMin.HasValue) query = query.Where(l => l.CarDetails!.Mileage >= f.MileageMin);
        if (f.MileageMax.HasValue) query = query.Where(l => l.CarDetails!.Mileage <= f.MileageMax);
        if (f.Fuel.HasValue) query = query.Where(l => l.CarDetails!.Fuel == f.Fuel);
        if (f.Transmission.HasValue) query = query.Where(l => l.CarDetails!.Transmission == f.Transmission);
        if (f.Body.HasValue) query = query.Where(l => l.CarDetails!.Body == f.Body);
        if (f.Drive.HasValue) query = query.Where(l => l.CarDetails!.Drive == f.Drive);
        if (f.EngineCcMin.HasValue) query = query.Where(l => l.CarDetails!.EngineCc >= f.EngineCcMin);
        if (f.EngineCcMax.HasValue) query = query.Where(l => l.CarDetails!.EngineCc <= f.EngineCcMax);
        if (f.PowerMin.HasValue) query = query.Where(l => l.CarDetails!.PowerHp >= f.PowerMin);
        if (f.PowerMax.HasValue) query = query.Where(l => l.CarDetails!.PowerHp <= f.PowerMax);
        if (!string.IsNullOrWhiteSpace(f.Color)) query = query.Where(l => EF.Functions.Like(l.CarDetails!.Color!, $"%{f.Color}%"));
        if (f.EmissionStandard.HasValue) query = query.Where(l => l.CarDetails!.EmissionStandard >= f.EmissionStandard);
        if (f.AccidentFreeOnly) query = query.Where(l => l.CarDetails!.AccidentFree);
        if (f.ServiceBookOnly) query = query.Where(l => l.CarDetails!.ServiceBook);

        if (f.Options.Count > 0)
        {
            var required = f.Options.Aggregate(CarOptions.None, (acc, o) => acc | o);
            query = query.Where(l => (l.CarDetails!.Options & required) == required);
        }
        return query;
    }

    private static IQueryable<Listing> ApplyRealEstateFilters(IQueryable<Listing> query, SearchFilters f)
    {
        if (!f.HasRealEstateFilters) return query;

        query = query.Where(l => l.RealEstateDetails != null);
        if (f.PropertyType.HasValue) query = query.Where(l => l.RealEstateDetails!.PropertyType == f.PropertyType);
        if (f.Transaction.HasValue) query = query.Where(l => l.RealEstateDetails!.Transaction == f.Transaction);
        if (f.RoomsMin.HasValue) query = query.Where(l => l.RealEstateDetails!.Rooms >= f.RoomsMin);
        if (f.RoomsMax.HasValue) query = query.Where(l => l.RealEstateDetails!.Rooms <= f.RoomsMax);
        if (f.SurfaceMin.HasValue) query = query.Where(l => l.RealEstateDetails!.SurfaceM2 >= f.SurfaceMin);
        if (f.SurfaceMax.HasValue) query = query.Where(l => l.RealEstateDetails!.SurfaceM2 <= f.SurfaceMax);
        if (f.FloorMin.HasValue) query = query.Where(l => l.RealEstateDetails!.Floor >= f.FloorMin);
        if (f.FloorMax.HasValue) query = query.Where(l => l.RealEstateDetails!.Floor <= f.FloorMax);
        if (f.YearBuiltMin.HasValue) query = query.Where(l => l.RealEstateDetails!.YearBuilt >= f.YearBuiltMin);
        if (f.YearBuiltMax.HasValue) query = query.Where(l => l.RealEstateDetails!.YearBuilt <= f.YearBuiltMax);
        if (f.Heating.HasValue) query = query.Where(l => l.RealEstateDetails!.Heating == f.Heating);
        if (f.Furnished.HasValue) query = query.Where(l => l.RealEstateDetails!.Furnished == f.Furnished);

        if (f.Amenities.Count > 0)
        {
            var required = f.Amenities.Aggregate(PropertyAmenities.None, (acc, a) => acc | a);
            query = query.Where(l => (l.RealEstateDetails!.Amenities & required) == required);
        }
        return query;
    }

    public async Task<List<int>> GetCategoryWithDescendantsAsync(string slug)
    {
        var all = await db.Categories.AsNoTracking().Select(c => new { c.Id, c.ParentId, c.Slug }).ToListAsync();
        var root = all.FirstOrDefault(c => c.Slug == slug);
        if (root is null) return [];

        var byParent = all.GroupBy(c => c.ParentId).ToDictionary(g => g.Key ?? 0, g => g.Select(x => x.Id).ToList());
        var result = new List<int>();
        var stack = new Stack<int>();
        stack.Push(root.Id);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            result.Add(id);
            if (byParent.TryGetValue(id, out var children))
                foreach (var c in children) stack.Push(c);
        }
        return result;
    }
}
