namespace LearnHub.Models.DTOs.Certificate
{
    public class CertificateDto
    {
        public long Id { get; set; }
        public long EnrollmentId { get; set; }
        public string CertificateUrl { get; set; }
        public DateTime IssuedAt { get; set; }
        public string CourseTitle { get; set; }
        public string StudentUsername { get; set; }
    }
}
