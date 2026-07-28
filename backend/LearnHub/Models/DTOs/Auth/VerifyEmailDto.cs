using System.ComponentModel.DataAnnotations;

namespace LearnHub.Models.DTOs.Auth
{
    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; }
    }
}
