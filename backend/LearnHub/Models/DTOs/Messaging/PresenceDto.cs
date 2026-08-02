namespace LearnHub.Models.DTOs.Messaging
{
    public class PresenceDto
    {
        public long UserId { get; set; }
        public string Status { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }
}
