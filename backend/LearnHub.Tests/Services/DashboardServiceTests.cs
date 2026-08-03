using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class DashboardServiceTests
    {
        private static (AppDbContext Db, DashboardService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var sut = new DashboardService(db, new EnrollmentService(db), new InstructorApplicationService(db));
            return (db, sut);
        }

        private static User SeedUser(AppDbContext db, string email, Role role = Role.Student)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = role,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static Course SeedCourse(AppDbContext db, long instructorId, CourseStatus status)
        {
            var course = new Course
            {
                InstructorId = instructorId,
                Title = "Intro to Testing",
                Description = "Learn how to write unit tests",
                Status = status,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        private static Enrollment SeedEnrollment(AppDbContext db, long studentId, long courseId, DateTime? completedAt = null)
        {
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                CompletedAt = completedAt,
                EnrolledAt = DateTime.UtcNow,
            };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
            return enrollment;
        }

        private static void SeedCertificate(AppDbContext db, long enrollmentId)
        {
            db.Certificates.Add(new Certificate
            {
                EnrollmentId = enrollmentId,
                CertificateUrl = "https://example.com/cert.pdf",
                IssuedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        private static void SeedApplication(AppDbContext db, long userId, ApplicationStatus status)
        {
            db.InstructorApplications.Add(new Models.Entities.InstructorApplication
            {
                UserId = userId,
                Message = "Let me teach!",
                Status = status,
                SubmittedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        // ----- GetStudentDashboardAsync -----

        [Fact]
        public async Task GetStudentDashboardAsync_MixedEnrollments_ReturnsAccurateCounts()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course1 = SeedCourse(db, instructor.Id, CourseStatus.Published);
            var course2 = SeedCourse(db, instructor.Id, CourseStatus.Published);
            var completedEnrollment = SeedEnrollment(db, student.Id, course1.Id, completedAt: DateTime.UtcNow);
            SeedEnrollment(db, student.Id, course2.Id, completedAt: null);
            SeedCertificate(db, completedEnrollment.Id);

            var result = await sut.GetStudentDashboardAsync(student.Id);

            result.TotalEnrollments.Should().Be(2);
            result.CompletedCount.Should().Be(1);
            result.InProgressCount.Should().Be(1);
            result.CertificateCount.Should().Be(1);
            result.Enrollments.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetStudentDashboardAsync_NoApplication_StatusIsNull()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com");

            var result = await sut.GetStudentDashboardAsync(student.Id);

            result.InstructorApplicationStatus.Should().BeNull();
        }

        [Fact]
        public async Task GetStudentDashboardAsync_WithApplication_ReturnsLatestStatus()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com");
            SeedApplication(db, student.Id, ApplicationStatus.Rejected);

            var result = await sut.GetStudentDashboardAsync(student.Id);

            result.InstructorApplicationStatus.Should().Be(ApplicationStatus.Rejected);
        }

        // ----- GetInstructorDashboardAsync -----

        [Fact]
        public async Task GetInstructorDashboardAsync_ReturnsCountsByStatusAndAllCourses()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student1 = SeedUser(db, "student1@learnhub.com");
            var student2 = SeedUser(db, "student2@learnhub.com");
            var published = SeedCourse(db, instructor.Id, CourseStatus.Published);
            SeedCourse(db, instructor.Id, CourseStatus.Draft);
            SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);
            SeedEnrollment(db, student1.Id, published.Id);
            SeedEnrollment(db, student2.Id, published.Id);

            var result = await sut.GetInstructorDashboardAsync(instructor.Id);

            result.TotalCourses.Should().Be(3);
            result.PublishedCourseCount.Should().Be(1);
            result.DraftCourseCount.Should().Be(1);
            result.PendingApprovalCourseCount.Should().Be(1);
            result.RejectedCourseCount.Should().Be(0);
            result.TotalStudentsEnrolled.Should().Be(2);
            result.Courses.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetInstructorDashboardAsync_DoesNotIncludeOtherInstructorsCourses()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var otherInstructor = SeedUser(db, "other@learnhub.com", Role.Instructor);
            SeedCourse(db, instructor.Id, CourseStatus.Published);
            SeedCourse(db, otherInstructor.Id, CourseStatus.Published);

            var result = await sut.GetInstructorDashboardAsync(instructor.Id);

            result.TotalCourses.Should().Be(1);
        }
    }
}
