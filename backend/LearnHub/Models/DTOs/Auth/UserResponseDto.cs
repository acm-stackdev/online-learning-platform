using LearnHub.Models.Entities;

namespace LearnHub.Models.DTOs.Auth
{
    public class UserResponseDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public Role Role { get; set; }
        public string? AvatarUrl { get; set; }
        public string PresenceStatus { get; set; }
    }
}
