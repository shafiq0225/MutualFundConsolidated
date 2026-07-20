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
                logger.LogInformation(
                    "Admin user already exists — skipping seed.");
                return;
            }

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
    }
}