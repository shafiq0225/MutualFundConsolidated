using Microsoft.AspNetCore.Identity;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.API.Services
{
    public static class AdminSeedService
    {
        public static async Task SeedAdminAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<Program>>();

            var adminEmail = configuration["AdminSeed:Email"]
                ?? "admin@amfinav.com";
            var adminPassword = configuration["AdminSeed:Password"]
                ?? "Admin@2026!";
            var firstName = configuration["AdminSeed:FirstName"]
                ?? "System";
            var lastName = configuration["AdminSeed:LastName"]
                ?? "Admin";

            // Check if admin already exists
            var existing = await userManager.FindByEmailAsync(adminEmail);
            if (existing != null)
            {
                if (!await userManager.CheckPasswordAsync(existing, adminPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                    await userManager.ResetPasswordAsync(existing, token, adminPassword);
                    logger.LogInformation("Admin user password synced.");
                }
                else
                {
                    logger.LogInformation("Admin user already exists and password is valid.");
                }
            }
            else
            {
                var admin = new ApplicationUser
                {
                    Id = "ADMIN0000A",           // ← PAN = Id, same as real users
                    FirstName = firstName,
                    LastName = lastName,
                    Email = adminEmail,
                    UserName = adminEmail,
                    PanNumber = "ADMIN0000A",   // placeholder PAN for system admin
                    Role = UserRole.Admin,
                    UserType = UserType.None,
                    ApprovalStatus = ApprovalStatus.Approved,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                    logger.LogInformation(
                        "✅ Admin user seeded — Email={Email}", adminEmail);
                else
                    logger.LogError(
                        "❌ Admin seed failed: {Errors}",
                        string.Join(", ", result.Errors
                            .Select(e => e.Description)));
            }

            // ── Seed Demo User ────────────────────────────────────────────────
            var demoEmail = "demo@amfinav.com";
            var demoPassword = "DemoUser@2026!";
            var existingDemo = await userManager.FindByEmailAsync(demoEmail);

            if (existingDemo == null)
            {
                var demoUser = new ApplicationUser
                {
                    Id = "DEMO000000",
                    FirstName = "Public",
                    LastName = "Demo Visitor",
                    Email = demoEmail,
                    UserName = demoEmail,
                    PanNumber = "DEMO000000",
                    Role = UserRole.Admin,
                    UserType = UserType.None,
                    ApprovalStatus = ApprovalStatus.Approved,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };

                var demoResult = await userManager.CreateAsync(demoUser, demoPassword);
                if (demoResult.Succeeded)
                    logger.LogInformation("✅ Demo user seeded — Email={Email}", demoEmail);
                else
                    logger.LogError("❌ Demo user seed failed: {Errors}", string.Join(", ", demoResult.Errors.Select(e => e.Description)));
            }
        }
    }
}