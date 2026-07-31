using System.ComponentModel.DataAnnotations;
using LearnHub.Models.Entities;

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

        [Required]
        public Role? Role { get; set; }
    }
}
