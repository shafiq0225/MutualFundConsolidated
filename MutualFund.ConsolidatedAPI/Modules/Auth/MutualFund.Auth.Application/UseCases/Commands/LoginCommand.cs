using MutualFund.Auth.Application.DTOs.Auth;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class LoginCommand
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<LoginCommand> _logger;

        public LoginCommand(
            IAuthService authService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<LoginCommand> logger)
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<TokenResponseDto> ExecuteAsync(
            LoginDto dto, string? ipAddress = null)
        {
            var (accessToken, refreshToken) =
                await _authService.LoginAsync(
                    dto.Email, dto.Password, ipAddress);

            _logger.LogInformation(
                "User logged in — Email={Email}", dto.Email);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                    _jwtSettings.TokenExpiryMinutes),
                TokenType = "Bearer"
            };
        }
    }
}