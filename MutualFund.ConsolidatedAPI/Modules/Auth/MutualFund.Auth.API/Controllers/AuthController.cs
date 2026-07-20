using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MutualFund.Auth.Application.DTOs.Auth;
using MutualFund.Auth.Application.UseCases.Commands;

namespace MutualFund.Auth.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RegisterCommand _registerCommand;
        private readonly LoginCommand _loginCommand;
        private readonly RefreshTokenCommand _refreshCommand;
        private readonly LogoutCommand _logoutCommand;
        private readonly ChangePasswordCommand _changePasswordCommand;

        public AuthController(
            RegisterCommand registerCommand,
            LoginCommand loginCommand,
            RefreshTokenCommand refreshCommand,
            LogoutCommand logoutCommand,
            ChangePasswordCommand changePasswordCommand)
        {
            _registerCommand = registerCommand;
            _loginCommand = loginCommand;
            _refreshCommand = refreshCommand;
            _logoutCommand = logoutCommand;
            _changePasswordCommand = changePasswordCommand;
        }

        /// <summary>
        /// Register a new user. Account is pending until Admin approves.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _registerCommand.ExecuteAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Login with email and password.
        /// Returns JWT access token + refresh token.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var ipAddress = GetIpAddress();
            var result = await _loginCommand.ExecuteAsync(dto, ipAddress);
            return Ok(result);
        }

        /// <summary>
        /// Refresh an expired access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            var ipAddress = GetIpAddress();
            var result = await _refreshCommand.ExecuteAsync(
                dto.RefreshToken, ipAddress);
            return Ok(result);
        }

        /// <summary>
        /// Logout — revokes the refresh token.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
        {
            var userId = User.FindFirstValue("sub") ?? string.Empty;

            await _logoutCommand.ExecuteAsync(userId, dto.RefreshToken);
            return Ok(new { message = "Logged out successfully." });
        }

        /// <summary>
        /// Change password for the currently authenticated user.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue("sub") ?? string.Empty;

            await _changePasswordCommand.ExecuteAsync(userId, dto);
            return Ok(new { message = "Password changed successfully." });
        }

        private string GetIpAddress() =>
            Request.Headers.TryGetValue("X-Forwarded-For", out var ip)
                ? ip.ToString()
                : HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";
    }
}