using MutualFund.Investment.Domain.Interfaces;
using MutualFund.Investment.Infrastructure.BackgroundJobs;
using MutualFund.Investment.Infrastructure.Data;
using MutualFund.Investment.Infrastructure.Repositories;
using MutualFund.Investment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace MutualFund.Investment.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Database ───────────────────────────────────────────
            services.AddDbContext<InvestmentDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 35)),
                    sql =>
                    {
                        sql.MigrationsAssembly(
                            typeof(InvestmentDbContext).Assembly.FullName);
                        sql.MigrationsHistoryTable("__efmigrationshistory");

                        // ← Required for MySQL transient fault handling
                        sql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // ── Unit of Work + Repositories ────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── NAV Rate Service ───────────────────────────────────
            services.AddScoped<INavRateService, NavRateService>();

            // ── Storage Service ────────────────────────────────────
            var azureConnectionString =
                configuration["AzureStorage:ConnectionString"];

            if (string.IsNullOrWhiteSpace(azureConnectionString))
            {
                services.AddScoped<IBlobStorageService,
                    LocalFileStorageService>();
            }
            else
            {
                services.AddScoped<IBlobStorageService,
                    BlobStorageService>();
            }

            // ── Quartz Scheduler ─────────────────────────────────────
            // FIX: QuartzConfiguration.ConfigureJobs existed but was never
            // called from anywhere, and Quartz itself was never registered
            // in the DI container — so PortfolioSnapshotJob never ran, on
            // any schedule, regardless of SnapshotScheduleTime's value.
            services.AddQuartz(q =>
            {
                QuartzConfiguration.ConfigureJobs(q, configuration);
            });

            services.AddQuartzHostedService(opts =>
            {
                // Waits for running jobs to finish before shutdown
                // instead of aborting them mid-execution.
                opts.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}