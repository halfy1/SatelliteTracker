using Microsoft.EntityFrameworkCore;
using SatelliteTracker.Backend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SatelliteData> SatelliteData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SatelliteData>()
            .HasIndex(d => d.Timestamp);

        modelBuilder.Entity<SatelliteData>()
            .HasIndex(d => d.SatelliteSystem);
    }
}