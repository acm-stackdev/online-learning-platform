namespace LearnHub.Models.Entities
{
    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class InstructorApplication
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Message { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public long? ReviewedByUserId { get; set; }

        public User Applicant { get; set; }
        public User? ReviewedBy { get; set; }
    }
}
