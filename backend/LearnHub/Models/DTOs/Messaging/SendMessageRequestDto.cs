namespace LearnHub.Models.DTOs.Messaging
{
    public class SendMessageRequestDto
    {
        public long EnrollmentId { get; set; }
        public string Content { get; set; }
    }
}
