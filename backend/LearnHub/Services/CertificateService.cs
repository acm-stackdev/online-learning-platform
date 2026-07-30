using LearnHub.Data;
using LearnHub.Helpers;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Certificate;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class CertificateService
    {
        private readonly AppDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public CertificateService(AppDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task IssueForEnrollmentAsync(long enrollmentId)
        {
            var alreadyIssued = await _db.Certificates.AnyAsync(c => c.EnrollmentId == enrollmentId);
            if (alreadyIssued)
                return;

            var enrollment = await _db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment is null)
                throw new ApiException("Enrollment not found.", 404);

            var pdfBytes = CertificatePdfGenerator.Generate(
                enrollment.Student.Username,
                enrollment.Course.Title,
                enrollment.Course.Instructor.Username,
                DateTime.UtcNow);

            var fileName = $"certificate-{enrollmentId}.pdf";
            var certificateUrl = await _fileUploadService.UploadRawAsync(pdfBytes, fileName);

            _db.Certificates.Add(new Certificate
            {
                EnrollmentId = enrollmentId,
                CertificateUrl = certificateUrl,
                IssuedAt = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync();
        }

        public async Task<CertificateDto> GetForEnrollmentAsync(long enrollmentId, long requesterId, bool isAdmin)
        {
            var certificate = await _db.Certificates
                .Include(c => c.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(c => c.Enrollment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);

            if (certificate is null)
                throw new ApiException("Certificate not found.", 404);

            if (!isAdmin && certificate.Enrollment.StudentId != requesterId)
                throw new ApiException("This is not your certificate.", 403);

            return new CertificateDto
            {
                Id = certificate.Id,
                EnrollmentId = certificate.EnrollmentId,
                CertificateUrl = certificate.CertificateUrl,
                IssuedAt = certificate.IssuedAt,
                CourseTitle = certificate.Enrollment.Course.Title,
                StudentUsername = certificate.Enrollment.Student.Username,
            };
        }
    }
}
