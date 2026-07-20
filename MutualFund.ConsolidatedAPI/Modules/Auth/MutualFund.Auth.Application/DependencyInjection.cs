using Microsoft.Extensions.DependencyInjection;
using MutualFund.Auth.Application.UseCases.Commands;
using MutualFund.Auth.Application.UseCases.Queries;

namespace MutualFund.Auth.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            // Auth commands
            services.AddScoped<RegisterCommand>();
            services.AddScoped<LoginCommand>();
            services.AddScoped<RefreshTokenCommand>();
            services.AddScoped<LogoutCommand>();
            services.AddScoped<ChangePasswordCommand>();

            // User commands
            services.AddScoped<ApproveUserCommand>();
            services.AddScoped<RejectUserCommand>();
            services.AddScoped<UpdateRoleCommand>();

            // Permission commands
            services.AddScoped<AssignPermissionCommand>();
            services.AddScoped<RevokePermissionCommand>();

            // Family commands
            services.AddScoped<CreateFamilyGroupCommand>();
            services.AddScoped<AddFamilyMemberCommand>();
            services.AddScoped<RemoveFamilyMemberCommand>();

            // Queries
            services.AddScoped<GetUsersQuery>();
            services.AddScoped<GetPermissionsQuery>();
            services.AddScoped<GetFamilyGroupsQuery>();

            return services;
        }
    }
}