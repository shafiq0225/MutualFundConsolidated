using System.ComponentModel.DataAnnotations;

namespace MutualFund.Auth.Application.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}