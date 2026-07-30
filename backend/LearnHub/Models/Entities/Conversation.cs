namespace LearnHub.Models.Entities
{
    public class Conversation
    {
        public long Id { get; set; }
        public long EnrollmentId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Enrollment Enrollment { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
