namespace LearnHub.Models
{
    public class Certificate
    {
    public long Id { get; set; }
    public long EnrollmentId { get; set; }
    public string CertificateUrl { get; set; }
    public DateTime IssuedAt { get; set; }
    
    public Enrollment Enrollment { get; set; }
}
}