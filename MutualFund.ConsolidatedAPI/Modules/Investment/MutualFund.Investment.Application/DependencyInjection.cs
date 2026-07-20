using MutualFund.Investment.Application.Family.Queries;
using MutualFund.Investment.Application.Orders.Commands;
using MutualFund.Investment.Application.Orders.Queries;
using MutualFund.Investment.Application.Portfolio.Commands;
using MutualFund.Investment.Application.Portfolio.Queries;
using MutualFund.Investment.Application.Statements.Commands;
using MutualFund.Investment.Application.Statements.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MutualFund.Investment.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            // ── Order Commands ─────────────────────────────────────
            services.AddScoped<CreateOrderCommand>();
            services.AddScoped<UpdateOrderStatusCommand>();

            // ── Order Queries ──────────────────────────────────────
            services.AddScoped<GetAllOrdersQuery>();
            services.AddScoped<GetOrderByIdQuery>();

            // ── Portfolio Commands ─────────────────────────────────
            services.AddScoped<CalculateSnapshotCommand>();

            // ── Portfolio Queries ──────────────────────────────────
            services.AddScoped<GetPortfolioQuery>();
            services.AddScoped<GetFamilyPortfolioQuery>();
            services.AddScoped<GetAllHoldingsQuery>();

            // ── Statement Commands ─────────────────────────────────
            services.AddScoped<UploadStatementCommand>();

            // ── Statement Queries ──────────────────────────────────
            services.AddScoped<GetStatementsQuery>();
            services.AddScoped<DownloadStatementQuery>();


            services.AddScoped<FamilyPortfolioQuery>();

            return services;
        }
    }
}