using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Auth
{
    public class RegisterDto
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }
    }
}
