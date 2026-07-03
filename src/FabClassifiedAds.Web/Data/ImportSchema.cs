using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Data;

/// <summary>
/// The app provisions its schema with EnsureCreated (no migrations), which does
/// nothing on a database that already exists. So tables/columns added after the
/// first deploy would never appear on production. This adds them explicitly and
/// idempotently — a no-op on fresh DBs, a safe add on existing ones (no data loss).
/// The DDL mirrors exactly what EF generates for the model.
/// </summary>
public static class ImportSchema
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ImportedListings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ImportedListings" PRIMARY KEY AUTOINCREMENT,
                "Source" TEXT NOT NULL,
                "ExternalId" TEXT NOT NULL,
                "SourceUrl" TEXT NULL,
                "Title" TEXT NOT NULL,
                "Description" TEXT NULL,
                "Price" decimal(18,2) NULL,
                "Currency" TEXT NOT NULL,
                "City" TEXT NULL,
                "Region" TEXT NULL,
                "CategorySlug" TEXT NULL,
                "ImagesJson" TEXT NULL,
                "AttributesJson" TEXT NULL,
                "ContentHash" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "PublishedListingId" INTEGER NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ImportedListings_Source_ExternalId\" ON \"ImportedListings\" (\"Source\", \"ExternalId\");");
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS \"IX_ImportedListings_Status\" ON \"ImportedListings\" (\"Status\");");

        // per-account API key columns (added after the first release)
        await AddColumnIfMissingAsync(db, "AspNetUsers", "ApiKeyHash", "TEXT");
        await AddColumnIfMissingAsync(db, "AspNetUsers", "ApiKeyCreatedAt", "TEXT");

        // moderation note (added with the admin approval queue)
        await AddColumnIfMissingAsync(db, "Listings", "ModerationNote", "TEXT");

        // one-time correction: the API/import system accounts and their listings are not
        // businesses (an earlier version marked them as such)
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"AspNetUsers\" SET \"IsBusiness\" = 0 WHERE \"Email\" IN ('api@aicigasesti.local','import@aicigasesti.local');");
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Listings\" SET \"SellerType\" = 0 WHERE \"UserId\" IN (SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"Email\" IN ('api@aicigasesti.local','import@aicigasesti.local'));");
    }

    private static async Task AddColumnIfMissingAsync(AppDbContext db, string table, string column, string type)
    {
        var existing = await db.Database
            .SqlQueryRaw<string>($"SELECT name AS \"Value\" FROM pragma_table_info('{table}')")
            .ToListAsync();
        if (!existing.Contains(column))
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type} NULL");
    }
}
