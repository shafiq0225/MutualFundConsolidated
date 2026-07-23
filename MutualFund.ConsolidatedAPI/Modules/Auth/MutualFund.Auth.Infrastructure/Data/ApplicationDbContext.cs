using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;
using System.Security;

namespace MutualFund.Auth.Infrastructure.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<FamilyGroup> FamilyGroups { get; set; }
        public DbSet<FamilyMember> FamilyMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── ApplicationUser ──────────────────────────────────────
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.LastName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.PanNumber)
                      .IsRequired()
                      .HasMaxLength(10);

                // PAN must be unique across all users
                entity.HasIndex(e => e.PanNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_PanNumber");

                entity.Property(e => e.Role)
                      .HasConversion<int>();

                entity.Property(e => e.UserType)
                      .HasConversion<int>();

                entity.Property(e => e.ApprovalStatus)
                      .HasConversion<int>();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // ── RefreshToken ─────────────────────────────────────────
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Token)
                      .IsUnique()
                      .HasDatabaseName("IX_RefreshTokens_Token");

                entity.Property(e => e.Token)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Permission ───────────────────────────────────────────
            builder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Code)
                      .IsUnique()
                      .HasDatabaseName("IX_Permissions_Code");

                entity.Property(e => e.Code)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // ── UserPermission ───────────────────────────────────────
            builder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);

                // One user cannot have the same permission twice (active)
                entity.HasIndex(e => new { e.UserId, e.PermissionId })
                      .HasDatabaseName("IX_UserPermissions_UserId_PermissionId");

                entity.Property(e => e.GrantedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserPermissions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.UserPermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── FamilyGroup ──────────────────────────────────────────
            builder.Entity<FamilyGroup>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.GroupName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.HeadUser)
                      .WithMany()
                      .HasForeignKey(e => e.HeadUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── FamilyMember ─────────────────────────────────────────
            builder.Entity<FamilyMember>(entity =>
            {
                entity.HasKey(e => e.Id);

                // A user can only be in one family group
                entity.HasIndex(e => e.UserId)
                      .IsUnique()
                      .HasDatabaseName("IX_FamilyMembers_UserId");

                entity.Property(e => e.AddedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.Property(e => e.RelationshipType)
                      .HasConversion<int>();

                entity.Property(e => e.DisplayLabel)
                      .HasMaxLength(100);

                entity.HasOne(e => e.FamilyGroup)
                      .WithMany(g => g.Members)
                      .HasForeignKey(e => e.FamilyGroupId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Seed Permissions ─────────────────────────────────────
            builder.Entity<Permission>().HasData(
                new Permission { Id = 1, Code = "scheme.manage", Name = "Manage Scheme Enrollment", Description = "Enroll, update, and approve schemes/funds", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 6, Code = "user.manage", Name = "Manage Users", Description = "Approve, reject and manage user accounts", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 7, Code = "family.manage", Name = "Manage Family Groups", Description = "Create and manage family groups", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 8, Code = "order.view", Name = "Manage Orders", Description = "View and manage orders", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 9, Code = "order.add", Name = "Add Orders", Description = "Log new orders", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 10, Code = "investor.view", Name = "Manage Investor Reports", Description = "View investor/portfolio reports", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Permission { Id = 11, Code = "investor.snapshot", Name = "Run Investor Snapshot", Description = "Run investor portfolio snapshot job", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // ── Map Identity tables explicitly to custom lowercase table names ────
            builder.Entity<ApplicationUser>().ToTable("users");
            builder.Entity<IdentityRole>().ToTable("roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("userroles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("userclaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("userlogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("roleclaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("usertokens");

            builder.Entity<RefreshToken>().ToTable("refreshtokens");
            builder.Entity<Permission>().ToTable("permissions");
            builder.Entity<UserPermission>().ToTable("userpermissions");
            builder.Entity<FamilyGroup>().ToTable("familygroups");
            builder.Entity<FamilyMember>().ToTable("familymembers");
        }
    }
}