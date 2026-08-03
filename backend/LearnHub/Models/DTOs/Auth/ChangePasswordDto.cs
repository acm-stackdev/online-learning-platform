using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required, MinLength(8)]
        public string NewPassword { get; set; }
    }
}
