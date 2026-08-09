using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearnHub.Tests.Controllers
{
    // [Authorize] is enforced by ASP.NET Core's authorization middleware, which never runs
    // when a test constructs CertificatesController directly - same documented limitation
    // as CoursesControllerTests.
    public class CertificatesControllerTests
    {
        private static (AppDbContext Db, CertificatesController Controller) CreateSut(System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock
                .Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            var controller = new CertificatesController(new CertificateService(db, fileUploadMock.Object))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(user)
            };
            return (db, controller);
        }

        private static User SeedUser(AppDbContext db, string email, Role role)
        {
            var user = new User { Username = email.Split('@')[0], Email = email, Role = role, IsEmailVerified = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static Enrollment SeedCompletedEnrollment(AppDbContext db, long studentId, long instructorId)
        {
            var course = new Course { InstructorId = instructorId, Title = "Intro to Testing", Description = "Learn how to write unit tests", Status = CourseStatus.Published, CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            db.SaveChanges();
            var enrollment = new Enrollment { StudentId = studentId, CourseId = course.Id, EnrolledAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
            return enrollment;
        }

        // ----- GET /api/certificates/{enrollmentId} -----

        [Fact]
        public async Task Get_Owner_ReturnsOk()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock.Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            await new CertificateService(db, fileUploadMock.Object).IssueForEnrollmentAsync(enrollment.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.Get(enrollment.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Get_NotTheStudent_Returns403()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var otherStudent = SeedUser(db, "other@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock.Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            await new CertificateService(db, fileUploadMock.Object).IssueForEnrollmentAsync(enrollment.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(otherStudent.Id, role: "Student"));

            var result = await controller.Get(enrollment.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task Get_UnknownEnrollment_Returns404()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Student"));

            var result = await controller.Get(12345);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }
    }
}
