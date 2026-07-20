using Microsoft.EntityFrameworkCore;
using MutualFund.Scheme.Domain.Entities;

namespace MutualFund.Scheme.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<SchemeEnrollment> SchemeEnrollments { get; set; }
        public DbSet<DetailedScheme> DetailedSchemes { get; set; }
        public DbSet<MarketHoliday> MarketHolidays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SchemeEnrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SchemeCode).IsUnique();
                entity.Property(e => e.SchemeCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SchemeName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FundName).IsRequired().HasMaxLength(500)
                      .HasDefaultValue(string.Empty);
                // ↑ Default lets the migration succeed against any existing
                // enrollment rows — backfill real FundName values afterward.
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            modelBuilder.Entity<DetailedScheme>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SchemeCode, e.NavDate }).IsUnique();
                entity.HasIndex(e => e.FundCode);
                entity.Property(e => e.FundCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FundName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.SchemeCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SchemeName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Nav).HasColumnType("decimal(18,4)");
                entity.Property(e => e.ReceivedAt).HasDefaultValueSql("UTC_TIMESTAMP()");
            });

            modelBuilder.Entity<MarketHoliday>(entity =>
            {
                entity.ToTable("SchemeApiMarketHolidays");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.HolidayDate).IsUnique();
                entity.Property(e => e.Source).HasMaxLength(100);
                entity.Property(e => e.ReceivedAt).HasDefaultValueSql("UTC_TIMESTAMP()");
            });
        }
    }
}