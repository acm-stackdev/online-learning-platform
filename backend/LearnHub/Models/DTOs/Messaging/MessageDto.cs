namespace LearnHub.Models.DTOs.Messaging
{
    public class MessageDto
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public long SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
