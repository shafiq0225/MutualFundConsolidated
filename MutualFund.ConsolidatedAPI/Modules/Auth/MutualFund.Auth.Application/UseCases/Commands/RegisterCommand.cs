using MutualFund.Auth.Application.DTOs.Auth;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class RegisterCommand
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RegisterCommand> _logger;

        public RegisterCommand(
            IAuthService authService,
            ILogger<RegisterCommand> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<RegisterResponseDto> ExecuteAsync(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                throw new Domain.Exceptions.AuthException(
                    "Password and confirm password do not match.",
                    "PASSWORD_MISMATCH", 400);

            var user = await _authService.RegisterAsync(
                dto.FirstName,
                dto.LastName,
                dto.Email,
                dto.Password,
                dto.PanNumber);

            _logger.LogInformation(
                "New user registered — Email={Email} PanNumber={Pan}",
                user.Email, user.PanNumber);

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PanNumber = user.PanNumber,
                Status = user.ApprovalStatus.ToString(),
                Message = "Registration successful. " +
                            "Your account is pending admin approval. " +
                            "You will be notified once approved."
            };
        }
    }
}