namespace LearnHub.Models.DTOs.Enrollment
{
    public class EnrollmentRosterItemDto
    {
        public long EnrollmentId { get; set; }
        public long StudentId { get; set; }
        public string StudentUsername { get; set; }
        public string StudentEmail { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
