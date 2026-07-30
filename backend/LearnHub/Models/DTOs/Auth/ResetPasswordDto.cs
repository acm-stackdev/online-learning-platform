using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required, MinLength(8)]
        public string NewPassword { get; set; }
    }
}
