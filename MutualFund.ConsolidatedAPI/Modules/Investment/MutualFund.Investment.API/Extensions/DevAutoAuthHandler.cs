using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MutualFund.Investment.API.Extensions
{
    /// <summary>
    /// Auto-authenticates every request as a fixed Admin identity —
    /// no token required at all.
    ///
    /// ONLY ever wired up when BOTH are true:
    ///   1. app.Environment.IsDevelopment()
    ///   2. AppSettings:DisableAuthForLocalTesting == true in config
    ///
    /// See JwtExtensions.AddJwtAuthentication for the gate. This class
    /// is never referenced/activated outside that condition, so it
    /// cannot accidentally end up protecting a real deployment.
    /// </summary>
    public class DevAutoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "DevAutoAuth";

        public DevAutoAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim("sub", "dev-local-admin"),
                new Claim("role", "Admin"),
                new Claim("firstName", "Dev"),
                new Claim("lastName", "Local")
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            Logger.LogWarning(
                "⚠ DevAutoAuthHandler is ACTIVE — all requests are being " +
                "auto-authenticated as Admin. This must NEVER be enabled " +
                "outside local development.");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
