using MutualFund.Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MutualFund.Investment.Infrastructure.Data.Configurations
{
    public class PortfolioSnapshotConfiguration
        : IEntityTypeConfiguration<PortfolioSnapshot>
    {
        public void Configure(EntityTypeBuilder<PortfolioSnapshot> builder)
        {
            builder.ToTable("PortfolioSnapshots");

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

            // ── Snapshot Date ──────────────────────────────────────
            builder.Property(x => x.SnapshotDate)
                .IsRequired();

            // ── NAV + Calculated values ────────────────────────────
            builder.Property(x => x.CurrentNAV)
                .IsRequired()
                .HasColumnType("decimal(18,4)");

            builder.Property(x => x.InvestedAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.CurrentValue)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ProfitLoss)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ProfitLossPercent)
                .IsRequired()
                .HasColumnType("decimal(10,4)");

            // ── Audit ──────────────────────────────────────────────
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // ── Unique constraint: one snapshot per holding per day ─
            builder.HasIndex(x => new { x.HoldingId, x.SnapshotDate })
                .IsUnique();

            // ── Indexes ────────────────────────────────────────────
            builder.HasIndex(x => x.InvestorUserId);
            builder.HasIndex(x => x.SnapshotDate);
            builder.HasIndex(x => new { x.InvestorUserId, x.SnapshotDate });
        }
    }
}