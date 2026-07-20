using MutualFund.Auth.Application.DTOs.Auth;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class RefreshTokenCommand
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<RefreshTokenCommand> _logger;

        public RefreshTokenCommand(
            IAuthService authService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<RefreshTokenCommand> logger)
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<TokenResponseDto> ExecuteAsync(
            string refreshToken, string? ipAddress = null)
        {
            var (accessToken, newRefreshToken) =
                await _authService.RefreshTokenAsync(
                    refreshToken, ipAddress);

            _logger.LogInformation("Token refreshed successfully");

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                    _jwtSettings.TokenExpiryMinutes),
                TokenType = "Bearer"
            };
        }
    }
}