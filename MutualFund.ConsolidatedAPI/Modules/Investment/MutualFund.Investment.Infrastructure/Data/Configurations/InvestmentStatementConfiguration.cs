using MutualFund.Investment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MutualFund.Investment.Infrastructure.Data.Configurations
{
    public class InvestmentStatementConfiguration
        : IEntityTypeConfiguration<InvestmentStatement>
    {
        public void Configure(EntityTypeBuilder<InvestmentStatement> builder)
        {
            builder.ToTable("InvestmentStatements");

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

            // ── Statement ──────────────────────────────────────────
            builder.Property(x => x.StatementDate)
                .IsRequired();

            // ── File ───────────────────────────────────────────────
            builder.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.FileSizeBytes)
                .IsRequired();

            // ── Upload Audit ───────────────────────────────────────
            builder.Property(x => x.UploadedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.UploadedAt)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            // ── Indexes ────────────────────────────────────────────
            builder.HasIndex(x => x.InvestorUserId);
            builder.HasIndex(x => x.OrderId)
                .IsUnique();
            // One statement per order
        }
    }
}