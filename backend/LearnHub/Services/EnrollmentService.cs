using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Course;
using LearnHub.Models.DTOs.Enrollment;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services
{
    public class EnrollmentService
    {
        private readonly AppDbContext _db;

        public EnrollmentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<EnrollmentDto> EnrollAsync(long courseId, long studentId, Role studentRole)
        {
            if (studentRole == Role.Admin)
                throw new ApiException("Admins cannot enroll in courses.", 403);

            var course = await _db.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (course.InstructorId == studentId)
                throw new ApiException("You cannot enroll in your own course.", 400);

            if (course.Status != CourseStatus.Published)
                throw new ApiException("This course is not available for enrollment.", 400);

            var alreadyEnrolled = await _db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
            if (alreadyEnrolled)
                throw new ApiException("You are already enrolled in this course.", 409);

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                CompletedAt = null,
                EnrolledAt = DateTime.UtcNow,
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            return MapEnrollment(enrollment, course);
        }

        public async Task RemoveEnrollmentAsync(long enrollmentId, long requesterId, bool isAdmin)
        {
            var enrollment = await _db.Enrollments.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == enrollmentId);
            if (enrollment is null)
                throw new ApiException("Enrollment not found.", 404);

            var isEnrolledStudent = enrollment.StudentId == requesterId;
            var isOwningInstructor = enrollment.Course.InstructorId == requesterId;
            if (!isEnrolledStudent && !isOwningInstructor && !isAdmin)
                throw new ApiException("You are not allowed to remove this enrollment.", 403);

            _db.Enrollments.Remove(enrollment);
            await _db.SaveChangesAsync();
        }

        public async Task<List<EnrollmentRosterItemDto>> GetCourseRosterAsync(long courseId, long requesterId, bool isAdmin)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null)
                throw new ApiException("Course not found.", 404);

            if (!isAdmin && course.InstructorId != requesterId)
                throw new ApiException("You do not own this course.", 403);

            return await _db.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new EnrollmentRosterItemDto
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    StudentUsername = e.Student.Username,
                    StudentEmail = e.Student.Email,
                    EnrolledAt = e.EnrolledAt,
                    CompletedAt = e.CompletedAt,
                })
                .ToListAsync();
        }

        public async Task<List<EnrollmentDto>> GetMyEnrollmentsAsync(long studentId)
        {
            var enrollments = await _db.Enrollments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();

            return enrollments.Select(e => MapEnrollment(e, e.Course)).ToList();
        }

        private static EnrollmentDto MapEnrollment(Enrollment enrollment, Course course) => new()
        {
            Id = enrollment.Id,
            EnrolledAt = enrollment.EnrolledAt,
            CompletedAt = enrollment.CompletedAt,
            Course = new CourseListItemDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                ThumbnailUrl = course.ThumbnailUrl,
                Category = course.Category,
                Status = course.Status,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor.Username,
                CreatedAt = course.CreatedAt,
            },
        };
    }
}
