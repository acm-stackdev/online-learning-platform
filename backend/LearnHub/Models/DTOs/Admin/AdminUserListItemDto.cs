using LearnHub.Models.Entities;

namespace LearnHub.Models.DTOs.Admin
{
    public class AdminUserListItemDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public Role Role { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}
