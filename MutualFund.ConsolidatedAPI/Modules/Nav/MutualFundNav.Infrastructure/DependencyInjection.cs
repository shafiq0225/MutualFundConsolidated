using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Data;
using MutualFundNav.Infrastructure.Helpers;
using MutualFundNav.Infrastructure.Repositories;
using MutualFundNav.Infrastructure.Services;

namespace MutualFundNav.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Database (MutualFundDbV2) ──────────────────────────────────
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 35)),
                    b =>
                    {
                        b.MigrationsAssembly(
                            typeof(ApplicationDbContext).Assembly.FullName);
                        b.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // ── Repositories + UoW ─────────────────────────────────────────
            services.AddScoped<INavFileRepository, NavFileRepository>();
            services.AddScoped<IMarketHolidayRepository, MarketHolidayRepository>();
            services.AddScoped<IJobExecutionLogRepository, JobExecutionLogRepository>();
            services.AddScoped<IKafkaPublishLogRepository, KafkaPublishLogRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── HTTP Services ──────────────────────────────────────────────
            services.AddHttpClient<INavDownloadService, NavDownloadService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Add("User-Agent", "MutualFundNav-API/1.0");
            });

            services.AddHttpClient<INseHolidayFetcher, NseHolidayFetcher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Referer", "https://www.nseindia.com/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            });

            // ── Kafka (generic open publisher — one singleton per T) ───────
            services.AddSingleton(typeof(IKafkaPublisher<>), typeof(KafkaPublisher<>));

            // ── Helpers ────────────────────────────────────────────────────
            services.AddScoped<IDateHelper, DateHelper>();

            // ── Memory Cache ───────────────────────────────────────────────
            services.AddMemoryCache();

            return services;
        }
    }
}