namespace LearnHub.Models.DTOs.Messaging
{
    public class SendMessageResultDto
    {
        public MessageDto Message { get; set; }
        public long RecipientId { get; set; }
    }
}
