using Microsoft.Extensions.DependencyInjection;
using MutualFundNav.Application.UseCases.Commands;

namespace MutualFundNav.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<DownloadAndStoreNavCommand>();
            services.AddScoped<UpsertNavCommand>();
            return services;
        }
    }
}