using MutualFund.Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MutualFund.Investment.Infrastructure.Data.Configurations
{
    public class HoldingConfiguration
        : IEntityTypeConfiguration<Holding>
    {
        public void Configure(EntityTypeBuilder<Holding> builder)
        {
            builder.ToTable("holdings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            // ── Investor ───────────────────────────────────────────
            builder.Property(x => x.InvestorUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.InvestorName)
                .IsRequired()
                .HasMaxLength(200);

            // ── Scheme ─────────────────────────────────────────────
            builder.Property(x => x.SchemeCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.SchemeName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.FundName)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.FolioNumber)
                .IsRequired()
                .HasMaxLength(50);

            // ── Purchase Details ───────────────────────────────────
            builder.Property(x => x.PurchaseDate)
                .IsRequired();

            builder.Property(x => x.PurchaseNAV)
                .IsRequired()
                .HasColumnType("decimal(18,4)");

            builder.Property(x => x.InvestedAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Units)
                .IsRequired()
                .HasColumnType("decimal(18,6)");

            // ── Status ─────────────────────────────────────────────
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // ── Audit ──────────────────────────────────────────────
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // ── Indexes ────────────────────────────────────────────
            builder.HasIndex(x => x.InvestorUserId);
            builder.HasIndex(x => x.SchemeCode);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => new { x.InvestorUserId, x.IsActive });

            // ── Relationships ──────────────────────────────────────
            builder.HasMany(x => x.Snapshots)
                .WithOne(s => s.Holding)
                .HasForeignKey(s => s.HoldingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}