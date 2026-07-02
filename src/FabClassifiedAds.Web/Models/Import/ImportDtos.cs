using System.ComponentModel.DataAnnotations;

namespace FabClassifiedAds.Web.Models.Import;

/// <summary>One normalized listing pushed by an external collector into the staging area.</summary>
public class ImportListingDto
{
    [Required, MaxLength(120)] public string ExternalId { get; set; } = "";
    [MaxLength(40)] public string? Source { get; set; }             // defaults to the request-level source
    [MaxLength(500)] public string? SourceUrl { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }

    public decimal? Price { get; set; }
    [MaxLength(8)] public string? Currency { get; set; }

    [MaxLength(120)] public string? City { get; set; }
    [MaxLength(120)] public string? Region { get; set; }
    [MaxLength(160)] public string? CategorySlug { get; set; }

    public List<string>? Images { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

public class ImportBatchDto
{
    [MaxLength(40)] public string? Source { get; set; }            // applied to items without their own Source
    [Required] public List<ImportListingDto> Listings { get; set; } = [];
}

public class ImportResult
{
    public int Received { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public List<string> Errors { get; set; } = [];
}
