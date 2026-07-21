using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MutualFund.Investment.Infrastructure.Data.Configurations
{
    public class InvestmentOrderConfiguration
        : IEntityTypeConfiguration<InvestmentOrder>
    {
        public void Configure(EntityTypeBuilder<InvestmentOrder> builder)
        {
            builder.ToTable("investmentorders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            // ── Order Identity ─────────────────────────────────────
            builder.Property(x => x.OrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.OrderNumber)
                .IsUnique();

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

            // ── Amount ─────────────────────────────────────────────
            builder.Property(x => x.InvestedAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // ── Payment ────────────────────────────────────────────
            builder.Property(x => x.PaymentMode)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ChequeNumber)
                .HasMaxLength(50);

            builder.Property(x => x.BankName)
                .HasMaxLength(200);

            builder.Property(x => x.TransactionRef)
                .HasMaxLength(100);

            // ── Status ─────────────────────────────────────────────
            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // ── Submission ─────────────────────────────────────────
            builder.Property(x => x.SubmittedByUserId)
                .HasMaxLength(450);

            // ── Assignment ─────────────────────────────────────────
            builder.Property(x => x.AssignedStaffName)
                .HasMaxLength(200);

            // ── Verification ───────────────────────────────────────
            builder.Property(x => x.VerifiedByUserId)
                .HasMaxLength(450);

            // ── Valuation ──────────────────────────────────────────
            builder.Property(x => x.PurchaseNAV)
                .HasColumnType("decimal(18,4)");

            builder.Property(x => x.UnitsAllotted)
                .HasColumnType("decimal(18,6)");

            builder.Property(x => x.FolioNumber)
                .HasMaxLength(50);

            // ── Notes ──────────────────────────────────────────────
            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            // ── Audit ──────────────────────────────────────────────
            builder.Property(x => x.CreatedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // ── Indexes ────────────────────────────────────────────
            builder.HasIndex(x => x.InvestorUserId);
            builder.HasIndex(x => x.SchemeCode);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.OrderDate);

            // ── Relationships ──────────────────────────────────────
            builder.HasOne(x => x.Holding)
                .WithOne(h => h.Order)
                .HasForeignKey<Holding>(h => h.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Statement)
                .WithOne(s => s.Order)
                .HasForeignKey<InvestmentStatement>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}