using MutualFund.Auth.Domain.Entities;

namespace MutualFund.Auth.Domain.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user. Account starts as Pending —
        /// user cannot log in until Admin approves.
        /// </summary>
        Task<ApplicationUser> RegisterAsync(
            string firstName,
            string lastName,
            string email,
            string password,
            string panNumber);

        /// <summary>
        /// Authenticates user credentials. Checks approval status
        /// before issuing tokens.
        /// Returns access token + refresh token.
        /// </summary>
        Task<(string AccessToken, string RefreshToken)> LoginAsync(
            string email,
            string password,
            string? ipAddress = null);

        /// <summary>
        /// Validates the refresh token and issues a new access token
        /// + rotated refresh token.
        /// </summary>
        Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(
            string refreshToken,
            string? ipAddress = null);

        /// <summary>
        /// Revokes the refresh token — user is logged out.
        /// </summary>
        Task LogoutAsync(string userId, string refreshToken);

        /// <summary>
        /// Changes the user's password after validating
        /// the current password.
        /// </summary>
        Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword);
    }
}