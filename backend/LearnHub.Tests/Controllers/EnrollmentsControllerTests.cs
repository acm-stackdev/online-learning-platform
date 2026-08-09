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
    // [Authorize(Roles = "...")] is enforced by ASP.NET Core's authorization middleware,
    // which never runs when a test constructs EnrollmentsController directly - same
    // documented limitation as CoursesControllerTests. Scoped to GetProgress and Delete
    // (Remove) only - Enrol/GetMine/GetRoster controller tests are a separate, known gap
    // not part of this pass.
    public class EnrollmentsControllerTests
    {
        private static (AppDbContext Db, EnrollmentsController Controller) CreateSut(System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock.Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            var enrollmentService = new EnrollmentService(db);
            var progressService = new ProgressService(db, new CertificateService(db, fileUploadMock.Object));
            var controller = new EnrollmentsController(enrollmentService, progressService)
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

        private static Course SeedCourse(AppDbContext db, long instructorId)
        {
            var course = new Course { InstructorId = instructorId, Title = "Intro to Testing", Description = "Learn how to write unit tests", Status = CourseStatus.Published, CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        private static Enrollment SeedEnrollment(AppDbContext db, long studentId, long courseId)
        {
            var enrollment = new Enrollment { StudentId = studentId, CourseId = courseId, EnrolledAt = DateTime.UtcNow };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
            return enrollment;
        }

        // ----- GET /api/enrollments/{id}/progress -----

        [Fact]
        public async Task GetProgress_Owner_ReturnsOk()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetProgress(enrollment.Id);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetProgress_NotYourEnrollment_Returns403()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var otherStudent = SeedUser(db, "other@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(otherStudent.Id, role: "Student"));

            var result = await controller.GetProgress(enrollment.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetProgress_UnknownEnrollment_Returns404()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Student"));

            var result = await controller.GetProgress(12345);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }

        // ----- DELETE /api/enrollments/{id} -----

        [Fact]
        public async Task Delete_EnrolledStudent_ReturnsNoContent()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.Delete(enrollment.Id);

            result.Should().BeOfType<NoContentResult>();
            db.Enrollments.Any(e => e.Id == enrollment.Id).Should().BeFalse();
        }

        [Fact]
        public async Task Delete_OwningInstructor_ReturnsNoContent()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));

            var result = await controller.Delete(enrollment.Id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_Admin_ReturnsNoContent()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(999, role: "Admin"));
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var result = await controller.Delete(enrollment.Id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_UnrelatedUser_Returns403()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var unrelatedStudent = SeedUser(db, "unrelated@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(unrelatedStudent.Id, role: "Student"));

            var result = await controller.Delete(enrollment.Id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task Delete_UnknownEnrollment_Returns404()
        {
            var (db, controller) = CreateSut(ControllerTestHelpers.BuildUserPrincipal(1, role: "Student"));

            var result = await controller.Delete(12345);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }
    }
}
