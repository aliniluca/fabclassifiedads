using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabClassifiedAds.Web.Models.Entities;

public enum ImportStatus { Pending = 0, Published = 1, Rejected = 2 }

/// <summary>
/// Staging row for externally-sourced listings. Nothing here is public until it is
/// reviewed and explicitly promoted to a real <see cref="Listing"/>. Keeps the raw
/// payload plus a content hash so re-imports of an unchanged source are skipped.
/// </summary>
public class ImportedListing
{
    public int Id { get; set; }

    [MaxLength(40)] public string Source { get; set; } = "";       // e.g. "olx", "partner-feed"
    [MaxLength(120)] public string ExternalId { get; set; } = "";   // id in the source system
    [MaxLength(500)] public string? SourceUrl { get; set; }

    [MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal? Price { get; set; }
    [MaxLength(8)] public string Currency { get; set; } = "EUR";

    [MaxLength(120)] public string? City { get; set; }
    [MaxLength(120)] public string? Region { get; set; }
    [MaxLength(160)] public string? CategorySlug { get; set; }      // hint mapped on publish

    public string? ImagesJson { get; set; }                         // JSON array of URLs
    public string? AttributesJson { get; set; }                     // JSON object of extra fields

    [MaxLength(64)] public string ContentHash { get; set; } = "";

    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int? PublishedListingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
