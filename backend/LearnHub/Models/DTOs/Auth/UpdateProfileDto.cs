using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Auth
{
    public class UpdateProfileDto
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Username { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
