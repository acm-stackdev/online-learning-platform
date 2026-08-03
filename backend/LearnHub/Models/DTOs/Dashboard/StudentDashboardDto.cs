using LearnHub.Models.DTOs.Enrollment;
using LearnHub.Models.Entities;

namespace LearnHub.Models.DTOs.Dashboard
{
    public class StudentDashboardDto
    {
        public int TotalEnrollments { get; set; }
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CertificateCount { get; set; }
        public ApplicationStatus? InstructorApplicationStatus { get; set; }
        public List<EnrollmentDto> Enrollments { get; set; } = new();
    }
}
