namespace FabClassifiedAds.Web.Models.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Icon { get; set; } = "";          // emoji or css icon key
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
    public int SortOrder { get; set; }

    /// <summary>Which specialized detail form/filters this category uses.</summary>
    public CategoryKind Kind { get; set; } = CategoryKind.Generic;

    public List<Listing> Listings { get; set; } = [];
}

public enum CategoryKind
{
    Generic = 0,
    Cars = 1,
    RealEstate = 2,
    Motorcycles = 3,
    Jobs = 4,
    Electronics = 5,
    Fashion = 6,
}
