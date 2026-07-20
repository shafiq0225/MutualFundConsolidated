using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MutualFund.Investment.API.Extensions
{
    public static class JwtExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // ── Dev-only auth bypass ────────────────────────────────
            // Gated behind BOTH IsDevelopment() AND an explicit config
            // flag, so this can never accidentally activate outside
            // local testing. See DevAutoAuthHandler for details.
            var bypassAuthForLocalTesting =
                environment.IsDevelopment() &&
                configuration.GetValue<bool>(
                    "AppSettings:DisableAuthForLocalTesting");

            if (bypassAuthForLocalTesting)
            {
                services
                    .AddAuthentication(DevAutoAuthHandler.SchemeName)
                    .AddScheme<
                        Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                        DevAutoAuthHandler>(
                        DevAutoAuthHandler.SchemeName,
                        _ => { });
            }
            else
            {
                var jwtSection = configuration.GetSection("JwtSettings");
                var secretKey = jwtSection["SecretKey"]!;
                var issuer = jwtSection["Issuer"]!;
                var audience = jwtSection["Audience"]!;

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme =
                            JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme =
                            JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            // ← CRITICAL: prevents role claim remapping
                            options.MapInboundClaims = false;

                            options.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(secretKey)),
                                    ValidateIssuer = true,
                                    ValidIssuer = issuer,
                                    ValidateAudience = true,
                                    ValidAudience = audience,
                                    ValidateLifetime = true,
                                    ClockSkew = TimeSpan.Zero
                                };
                        });
            }

            // ── Authorization Policies ─────────────────────────────
            // Unchanged either way — both the real JWT and the dev
            // bypass produce identically-shaped "role" claims, so the
            // same policies work against both.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("role", "Admin"));

                options.AddPolicy("AdminOrEmployee", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("role", "Employee")));

                options.AddPolicy("AnyRole", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("role", "Employee") ||
                        ctx.User.HasClaim("role", "User")));

                options.AddPolicy("CanViewOrders", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("permissions", "order.view")));

                options.AddPolicy("CanCreateOrder", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        (ctx.User.HasClaim("role", "Employee") &&
                         ctx.User.HasClaim("permissions", "order.view") &&
                         ctx.User.HasClaim("permissions", "order.add"))));

                options.AddPolicy("CanViewAllOrders", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("permissions", "order.view")));

                options.AddPolicy("CanViewInvestorPage", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("permissions", "investor.view")));

                options.AddPolicy("CanViewAllPortfolio", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        ctx.User.HasClaim("permissions", "investor.view")));

                options.AddPolicy("CanRunSnapshot", policy =>
                    policy.RequireAssertion(ctx =>
                        ctx.User.HasClaim("role", "Admin") ||
                        (ctx.User.HasClaim("role", "Employee") &&
                         ctx.User.HasClaim("permissions", "investor.view") &&
                         ctx.User.HasClaim("permissions", "investor.snapshot"))));
            });

            return services;
        }
    }
}