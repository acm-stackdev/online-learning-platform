namespace LearnHub.Models.DTOs.Messaging
{
    public class ConversationListItemDto
    {
        public long EnrollmentId { get; set; }
        public long? ConversationId { get; set; }
        public long CourseId { get; set; }
        public string CourseTitle { get; set; }
        public long OtherPartyId { get; set; }
        public string OtherPartyUsername { get; set; }
        public string? OtherPartyAvatarUrl { get; set; }
        public string OtherPartyPresence { get; set; }
        public string? LastMessagePreview { get; set; }
        public long? LastMessageSenderId { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
