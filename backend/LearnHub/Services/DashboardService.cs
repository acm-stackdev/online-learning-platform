using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Models.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _db;
        private readonly EnrollmentService _enrollmentService;
        private readonly InstructorApplicationService _instructorApplicationService;

        public DashboardService(AppDbContext db, EnrollmentService enrollmentService, InstructorApplicationService instructorApplicationService)
        {
            _db = db;
            _enrollmentService = enrollmentService;
            _instructorApplicationService = instructorApplicationService;
        }

        public async Task<StudentDashboardDto> GetStudentDashboardAsync(long userId)
        {
            var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(userId);
            var applications = await _instructorApplicationService.GetMineAsync(userId);
            var certificateCount = await _db.Certificates.CountAsync(c => c.Enrollment.StudentId == userId);

            return new StudentDashboardDto
            {
                TotalEnrollments = enrollments.Count,
                CompletedCount = enrollments.Count(e => e.CompletedAt != null),
                InProgressCount = enrollments.Count(e => e.CompletedAt == null),
                CertificateCount = certificateCount,
                InstructorApplicationStatus = applications.FirstOrDefault()?.Status,
                Enrollments = enrollments,
            };
        }

        public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(long instructorId)
        {
            var courses = await _db.Courses
                .Include(c => c.Instructor)
                .Where(c => c.InstructorId == instructorId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var courseIds = courses.Select(c => c.Id).ToList();
            var totalStudents = await _db.Enrollments.CountAsync(e => courseIds.Contains(e.CourseId));

            return new InstructorDashboardDto
            {
                TotalCourses = courses.Count,
                DraftCourseCount = courses.Count(c => c.Status == CourseStatus.Draft),
                PendingApprovalCourseCount = courses.Count(c => c.Status == CourseStatus.PendingApproval),
                PublishedCourseCount = courses.Count(c => c.Status == CourseStatus.Published),
                RejectedCourseCount = courses.Count(c => c.Status == CourseStatus.Rejected),
                TotalStudentsEnrolled = totalStudents,
                Courses = courses.Select(c => new CourseListItemDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    ThumbnailUrl = c.ThumbnailUrl,
                    Category = c.Category,
                    Status = c.Status,
                    InstructorId = c.InstructorId,
                    InstructorName = c.Instructor.Username,
                    CreatedAt = c.CreatedAt,
                }).ToList(),
            };
        }
    }
}
