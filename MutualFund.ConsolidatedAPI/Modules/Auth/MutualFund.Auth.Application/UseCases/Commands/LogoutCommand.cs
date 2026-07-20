using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class LogoutCommand
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LogoutCommand> _logger;

        public LogoutCommand(
            IAuthService authService,
            ILogger<LogoutCommand> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task ExecuteAsync(string userId, string refreshToken)
        {
            await _authService.LogoutAsync(userId, refreshToken);
            _logger.LogInformation(
                "User logged out — UserId={UserId}", userId);
        }
    }
}