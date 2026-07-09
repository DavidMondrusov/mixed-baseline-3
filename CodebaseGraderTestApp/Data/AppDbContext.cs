using Microsoft.EntityFrameworkCore;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(200);
        });

        modelBuilder.Entity<UserProfileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // ── PASS V6.3.2: admin seed data removed ──────────────────────────
        // (No default admin account is seeded)
        // The following seed is commented out — currently NOT executed:
        // modelBuilder.Entity<UserProfileEntity>().HasData(
        //     new UserProfileEntity
        //     {
        //         Id = 2,
        //         Username = "root",
        //         Email = "root@example.com",
        //         Role = "Admin",
        //         IsAdmin = true,
        //     });
    }
}
