using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.API.Middleware
{
    /// <summary>
    /// Security Middleware that enforces Read-Only (GET-only) access for public Demo users.
    /// Blocks any POST, PUT, DELETE, or PATCH mutations with HTTP 403 Forbidden.
    /// </summary>
    public class ReadOnlyDemoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ReadOnlyDemoMiddleware> _logger;

        public ReadOnlyDemoMiddleware(RequestDelegate next, ILogger<ReadOnlyDemoMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User;
            var isDemoUser = user.HasClaim("isDemo", "true");
            var method = context.Request.Method.ToUpper();
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            // Allow GET / OPTIONS requests, and auth endpoints (login, demo-login, refresh, logout)
            if (isDemoUser && method != "GET" && method != "OPTIONS")
            {
                // Exempt authentication endpoints so demo user can manage their session
                if (!path.Contains("/api/auth/"))
                {
                    _logger.LogWarning("Blocked mutation attempt '{Method} {Path}' from Demo User.", method, path);

                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Response.ContentType = "application/json";

                    var responsePayload = new
                    {
                        errorCode = "DEMO_MODE_READ_ONLY",
                        message = "Demo Mode Enabled: Data modification is disabled in this public live showcase."
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
                    return;
                }
            }

            await _next(context);
        }
    }
}
