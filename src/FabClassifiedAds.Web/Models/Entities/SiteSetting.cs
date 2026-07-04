using System.ComponentModel.DataAnnotations;

namespace FabClassifiedAds.Web.Models.Entities;

/// <summary>A single editable-at-runtime setting (key → value). Backs the admin settings page.</summary>
public class SiteSetting
{
    [MaxLength(80)] public string Key { get; set; } = "";
    public string? Value { get; set; }
}
