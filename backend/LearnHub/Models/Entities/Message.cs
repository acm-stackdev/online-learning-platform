namespace LearnHub.Models.Entities
{
    public class Message
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public long SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public Conversation Conversation { get; set; }
        public User Sender { get; set; }
    }
}
