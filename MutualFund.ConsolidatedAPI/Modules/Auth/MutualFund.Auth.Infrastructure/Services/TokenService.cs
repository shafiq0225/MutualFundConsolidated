using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Data;
using MutualFund.Auth.Infrastructure.Settings;

namespace MutualFund.Auth.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwt;
        private readonly ApplicationDbContext _context;

        public TokenService(
            IOptions<JwtSettings> jwt,
            ApplicationDbContext context)
        {
            _jwt = jwt.Value;
            _context = context;
        }

        public string GenerateAccessToken(
            ApplicationUser user,
            IEnumerable<string> permissions)
        {
            var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(
                            key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new("firstName",      user.FirstName),
                new("lastName",       user.LastName),
                new("role",           user.Role.ToString()),
                new("userType",       user.UserType.ToString()),
                new("approvalStatus", user.ApprovalStatus.ToString()),
                new("panNumber",      user.PanNumber),
            };

            // Add each permission as a separate claim
            foreach (var permission in permissions)
                claims.Add(new Claim("permissions", permission));

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(
                                        _jwt.TokenExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(
            string userId, string? ipAddress = null)
        {
            var token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));

            return new RefreshToken
            {
                Token = token,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(
                                   _jwt.RefreshTokenExpiryDays),
                CreatedByIp = ipAddress
            };
        }

        public string? GetUserIdFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwt.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,

                // ← Allow expired tokens for refresh flow
                ValidateLifetime = false
            };

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token,
                    tokenValidationParameters,
                    out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}