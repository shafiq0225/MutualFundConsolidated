using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Serilog;
using MutualFund.Auth.API.Middleware;

// Workers and services
using MutualFundNav.API.Workers;
using MutualFund.Auth.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ── Controllers (Load from all referenced projects) ────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .AddApplicationPart(typeof(MutualFund.Auth.API.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(MutualFund.Investment.API.Controllers.OrdersController).Assembly)
    .AddApplicationPart(typeof(MutualFund.Scheme.API.Controllers.SchemeEnrollmentController).Assembly)
    .AddApplicationPart(typeof(MutualFundNav.API.Controllers.NavController).Assembly);

builder.Services.AddEndpointsApiExplorer();

// ── Clean Architecture Layers ─────────────────────────────────────
// Auth
MutualFund.Auth.Application.DependencyInjection.AddApplication(builder.Services);
MutualFund.Auth.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

// Investment
MutualFund.Investment.Application.DependencyInjection.AddApplication(builder.Services);
MutualFund.Investment.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

// Scheme
MutualFund.Scheme.Application.DependencyInjection.AddApplication(builder.Services);
MutualFund.Scheme.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

// NAV
MutualFundNav.Application.DependencyInjection.AddApplication(builder.Services);
MutualFundNav.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

// ── Background Workers ─────────────────────────────────────────────
builder.Services.AddHostedService<NavDownloadWorker>();

// ── JWT Authentication ────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"]!;
var issuer = jwtSection["Issuer"]!;
var audience = jwtSection["Audience"]!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                ctx.Response.Headers.Append(
                    "Token-Expired",
                    ctx.Exception is SecurityTokenExpiredException ? "true" : "false");
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization Policies ────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    // Role-based
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("role", "Admin"));
    options.AddPolicy("EmployeeOrAbove", policy => policy.RequireClaim("role", "Admin", "Employee"));
    options.AddPolicy("AllRoles", policy => policy.RequireAuthenticatedUser());

    // Auth policies
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Admin") ||
            ctx.User.HasClaim("permissions", "UserManage")));

    options.AddPolicy("CanManageFamily", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Admin") ||
            ctx.User.HasClaim("permissions", "FamilyManage")));

    // Scheme policies
    options.AddPolicy("CanManageSchemeEnrollment", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Admin") ||
            ctx.User.HasClaim("permissions", "scheme.manage")));

    // ── Investment policies ─────────────────────────────────────────
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

// ── Swagger ───────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MutualFund Consolidated API",
        Version = "v1",
        Description = "Consolidated Monolith API hosting Auth, Scheme, Investment, and NAV services."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ── CORS ──────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ── Auto-migrate databases on startup ─────────────────────────────
using (var scope = app.Services.CreateScope())
{
    // 1. Auth Database
    try
    {
        var authDb = scope.ServiceProvider.GetRequiredService<MutualFund.Auth.Infrastructure.Data.ApplicationDbContext>();
        await authDb.Database.MigrateAsync();
        Log.Information("Auth database migrated successfully.");
        
        // Seed permissions & admin
        var newPerms = new[]
        {
            new MutualFund.Auth.Domain.Entities.Permission { Code = "order.view", Name = "Manage Orders", Description = "View and manage orders", CreatedAt = DateTime.UtcNow },
            new MutualFund.Auth.Domain.Entities.Permission { Code = "order.add", Name = "Add Orders", Description = "Log new orders", CreatedAt = DateTime.UtcNow },
            new MutualFund.Auth.Domain.Entities.Permission { Code = "investor.view", Name = "Manage Investor Reports", Description = "View investor/portfolio reports", CreatedAt = DateTime.UtcNow },
            new MutualFund.Auth.Domain.Entities.Permission { Code = "investor.snapshot", Name = "Run Investor Snapshot", Description = "Run investor portfolio snapshot job", CreatedAt = DateTime.UtcNow }
        };

        foreach (var perm in newPerms)
        {
            if (!await authDb.Permissions.AnyAsync(p => p.Code == perm.Code))
            {
                authDb.Permissions.Add(perm);
            }
        }
        await authDb.SaveChangesAsync();
        await AdminSeedService.SeedAdminAsync(app.Services, app.Configuration);
        Log.Information("Auth seed data verified.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate/seed Auth database.");
    }

    // 2. Investment Database
    try
    {
        var investDb = scope.ServiceProvider.GetRequiredService<MutualFund.Investment.Infrastructure.Data.InvestmentDbContext>();
        await investDb.Database.MigrateAsync();
        Log.Information("Investment database migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate Investment database.");
    }

    // 3. Scheme Database
    try
    {
        var schemeDb = scope.ServiceProvider.GetRequiredService<MutualFund.Scheme.Infrastructure.Data.ApplicationDbContext>();
        await schemeDb.Database.MigrateAsync();
        Log.Information("Scheme database migrated successfully.");

        // Seed default approved schemes
        var defaultSchemes = new[]
        {
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "105503",
                SchemeName = "Invesco India Midcap Fund - Regular Plan - Growth Option",
                FundName = "Invesco Mutual Fund",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "127039",
                SchemeName = "Motilal Oswal Midcap Fund - Regular Plan - Growth Option",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "101161",
                SchemeName = "Nippon India Multi Cap Fund - Growth Plan - Growth Option",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "124172",
                SchemeName = "DSP Banking & PSU Debt Fund - Regular Plan - Growth",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "111722",
                SchemeName = "CANARA ROBECO ELSS TAX SAVER - REGULAR PLAN - GROWTH OPTION",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "116077",
                SchemeName = "Invesco India Gold ETF Fund of Fund - Regular Plan - Growth",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "150641",
                SchemeName = "Motilal Oswal Gold and Silver Passive Fund of Funds(Regular Plan)",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "114616",
                SchemeName = "Nippon India Gold Savings Fund - Growth Plan - Growth Option",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            },
            new MutualFund.Scheme.Domain.Entities.SchemeEnrollment
            {
                SchemeCode = "133385",
                SchemeName = "Motilal Oswal ELSS Tax Saver Fund - Regular Plan - Growth Option",
                FundName = "",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var sch in defaultSchemes)
        {
            if (!await schemeDb.SchemeEnrollments.AnyAsync(s => s.SchemeCode == sch.SchemeCode))
            {
                schemeDb.SchemeEnrollments.Add(sch);
            }
        }
        await schemeDb.SaveChangesAsync();
        Log.Information("Scheme database seeded successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate/seed Scheme database.");
    }

    // 4. NAV Database
    try
    {
        var navDb = scope.ServiceProvider.GetRequiredService<MutualFundNav.Infrastructure.Data.ApplicationDbContext>();
        await navDb.Database.MigrateAsync();
        Log.Information("NAV database migrated successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to migrate NAV database.");
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────
// GlobalExceptionHandler MUST be first so it wraps the entire pipeline
// and returns JSON error responses to the frontend. Do NOT put
// UseDeveloperExceptionPage before it — that intercepts exceptions first
// and returns HTML, breaking the frontend's JSON error parsing.
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MutualFund Consolidated API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("MutualFund Consolidated API running...");
await app.RunAsync();
