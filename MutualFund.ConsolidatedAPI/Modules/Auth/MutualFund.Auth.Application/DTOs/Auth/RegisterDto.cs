namespace MutualFund.Auth.Application.DTOs.Auth
{
    public class RegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// PAN number — format: 5 letters + 4 digits + 1 letter
        /// Example: ABCDE1234F
        /// </summary>
        public string PanNumber { get; set; } = string.Empty;
    }
}