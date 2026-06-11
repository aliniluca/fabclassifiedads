using FabClassifiedAds.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FabClassifiedAds.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<ListingAttribute> ListingAttributes => Set<ListingAttribute>();
    public DbSet<CarBrand> CarBrands => Set<CarBrand>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<CarDetails> CarDetails => Set<CarDetails>();
    public DbSet<RealEstateDetails> RealEstateDetails => Set<RealEstateDetails>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasOne(c => c.Parent).WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Listing>(e =>
        {
            e.HasIndex(l => new { l.Status, l.CategoryId, l.BumpedAt });
            e.HasIndex(l => l.Price);
            e.HasOne(l => l.User).WithMany(u => u.Listings)
                .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CarDetails>(e =>
        {
            e.HasIndex(c => new { c.BrandId, c.ModelId, c.Year });
            e.HasOne(c => c.Listing).WithOne(l => l.CarDetails)
                .HasForeignKey<CarDetails>(c => c.ListingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Brand).WithMany().HasForeignKey(c => c.BrandId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Model).WithMany().HasForeignKey(c => c.ModelId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<RealEstateDetails>(e =>
        {
            e.HasIndex(r => new { r.PropertyType, r.Transaction, r.Rooms });
            e.HasOne(r => r.Listing).WithOne(l => l.RealEstateDetails)
                .HasForeignKey<RealEstateDetails>(r => r.ListingId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CarModel>().HasIndex(m => new { m.BrandId, m.Name }).IsUnique();
        b.Entity<CarBrand>().HasIndex(br => br.Slug).IsUnique();

        b.Entity<Favorite>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.ListingId }).IsUnique();
            e.HasOne(f => f.User).WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Listing).WithMany(l => l.Favorites)
                .HasForeignKey(f => f.ListingId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Conversation>(e =>
        {
            e.HasIndex(c => new { c.ListingId, c.BuyerId }).IsUnique();
            e.HasOne(c => c.Buyer).WithMany().HasForeignKey(c => c.BuyerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Seller).WithMany().HasForeignKey(c => c.SellerId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Message>()
            .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<SavedSearch>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.EmailAlerts });
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
