using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MutualFund.Scheme.Domain.Interfaces;
using MutualFund.Scheme.Infrastructure.Consumers;
using MutualFund.Scheme.Infrastructure.Data;

namespace MutualFund.Scheme.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
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

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            var kafkaEnabled = configuration.GetValue<bool>("Kafka:Enabled", true);
            if (kafkaEnabled)
            {
                services.AddHostedService<NavFileKafkaConsumer>();
                services.AddHostedService<MarketHolidayKafkaConsumer>();
            }

            return services;
        }
    }
}