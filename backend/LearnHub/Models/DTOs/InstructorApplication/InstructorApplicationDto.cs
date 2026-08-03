using LearnHub.Models.Entities;

namespace LearnHub.Models.DTOs.InstructorApplication
{
    public class InstructorApplicationDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string ApplicantUsername { get; set; }
        public string ApplicantEmail { get; set; }
        public string Message { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public long? ReviewedByUserId { get; set; }
        public string? ReviewedByUsername { get; set; }
    }
}
