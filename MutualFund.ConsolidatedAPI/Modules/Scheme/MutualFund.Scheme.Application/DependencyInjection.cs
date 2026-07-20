using Microsoft.Extensions.DependencyInjection;
using MutualFund.Scheme.Application.UseCases.Commands;
using MutualFund.Scheme.Application.UseCases.Queries;

namespace MutualFund.Scheme.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            // Commands
            services.AddScoped<CreateSchemeEnrollmentCommand>();
            services.AddScoped<UpdateSchemeEnrollmentCommand>();
            services.AddScoped<UpdateFundApprovalCommand>();

            // Queries
            services.AddScoped<GetHolidayStatusQuery>();
            services.AddScoped<GetSchemeEnrollmentsQuery>();
            services.AddScoped<GetNavComparisonQuery>();
            services.AddScoped<GetSchemeDetailsQuery>();

            return services;
        }
    }
}