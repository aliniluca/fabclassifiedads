using System.Collections.Concurrent;
using FabClassifiedAds.Web.Data;
using FabClassifiedAds.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Runtime-editable settings backed by the SiteSettings table, cached in memory.
/// Lets the admin change behavior (moderation hold-all, custom banned words) without
/// a redeploy. Keys fall back to IConfiguration / defaults when unset.
/// </summary>
public class SettingsStore(IServiceScopeFactory scopeFactory, IConfiguration config)
{
    public const string HoldAllKey = "Moderation:HoldAllNewListings";
    public const string BannedWordsKey = "Moderation:CustomBannedWords";   // profanity → Review
    public const string DangerWordsKey = "Moderation:CustomDangerWords";   // danger → Block

    private readonly ConcurrentDictionary<string, string?> _cache = new();
    private volatile bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_cache)
        {
            if (_loaded) return;
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var s in db.SiteSettings.AsNoTracking().ToList())
                _cache[s.Key] = s.Value;
            _loaded = true;
        }
    }

    public string? Get(string key)
    {
        EnsureLoaded();
        return _cache.TryGetValue(key, out var v) ? v : config[key];
    }

    public bool GetBool(string key, bool fallback = false)
    {
        var v = Get(key);
        return string.IsNullOrEmpty(v) ? config.GetValue(key, fallback) : v is "true" or "1" or "on";
    }

    public IReadOnlyList<string> GetWordList(string key)
    {
        var v = Get(key);
        return string.IsNullOrWhiteSpace(v)
            ? []
            : v.Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task SetAsync(string key, string? value)
    {
        EnsureLoaded();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (row is null) db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        else row.Value = value;
        await db.SaveChangesAsync();
        _cache[key] = value;
    }
}
