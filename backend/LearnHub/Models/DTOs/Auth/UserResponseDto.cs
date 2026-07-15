namespace LearnHub.Models.DTOs.Auth
{
    public class UserResponseDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public Role Role { get; set; }
    }
}
