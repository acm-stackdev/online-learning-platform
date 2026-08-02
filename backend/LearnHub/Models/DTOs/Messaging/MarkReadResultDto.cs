namespace LearnHub.Models.DTOs.Messaging
{
    public class MarkReadResultDto
    {
        public List<long> MessageIds { get; set; } = new();
        public long OtherPartyId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
