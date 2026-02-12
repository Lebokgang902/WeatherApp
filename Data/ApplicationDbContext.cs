using Microsoft.EntityFrameworkCore;
using WeatherApp.Models;

namespace WeatherApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Location> Locations { get; set; }
        public DbSet<WeatherSnapshot> WeatherSnapshots { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }  // ✅ NEW

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure city+country combination is unique - ONLY FOR NON-DELETED RECORDS
            modelBuilder.Entity<Location>()
                .HasIndex(l => new { l.City, l.Country })
                .IsUnique()
                .HasFilter("IsDeleted = 0");

            // Indexes for faster lookups
            modelBuilder.Entity<WeatherSnapshot>()
                .HasIndex(w => w.Timestamp);

            modelBuilder.Entity<WeatherSnapshot>()
                .HasIndex(w => w.ForecastDate);

            modelBuilder.Entity<WeatherSnapshot>()
                .HasIndex(w => w.LocationId);

            // Decimal precision
            modelBuilder.Entity<Location>()
                .Property(l => l.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<Location>()
                .Property(l => l.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<WeatherSnapshot>()
                .Property(w => w.Temperature)
                .HasPrecision(5, 2);

            modelBuilder.Entity<WeatherSnapshot>()
                .Property(w => w.FeelsLike)
                .HasPrecision(5, 2);

            modelBuilder.Entity<WeatherSnapshot>()
                .Property(w => w.WindSpeed)
                .HasPrecision(6, 2);

            // Configure Relationship - when Location is deleted, set LocationId to NULL
            modelBuilder.Entity<WeatherSnapshot>()
                .HasOne(w => w.Location)
                .WithMany(l => l.WeatherSnapshots)
                .HasForeignKey(w => w.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ NEW: User Preferences configuration
            modelBuilder.Entity<UserPreferences>()
                .HasIndex(p => p.Id)
                .IsUnique();

            // SOFT DELETE GLOBAL FILTER
            modelBuilder.Entity<Location>().HasQueryFilter(l => !l.IsDeleted);
        }
    }
}