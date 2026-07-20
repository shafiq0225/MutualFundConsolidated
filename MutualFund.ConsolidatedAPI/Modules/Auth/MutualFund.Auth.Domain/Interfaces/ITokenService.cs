using MutualFund.Auth.Domain.Entities;

namespace MutualFund.Auth.Domain.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Generates a signed JWT access token containing
        /// userId, email, role, userType and permissions claims.
        /// </summary>
        string GenerateAccessToken(
            ApplicationUser user,
            IEnumerable<string> permissions);

        /// <summary>
        /// Generates a cryptographically random refresh token
        /// and saves it to the database.
        /// </summary>
        RefreshToken GenerateRefreshToken(string userId, string? ipAddress = null);

        /// <summary>
        /// Extracts the userId claim from an expired (but validly signed) token.
        /// Used during refresh to identify which user is requesting a new token.
        /// </summary>
        string? GetUserIdFromExpiredToken(string token);
    }
}