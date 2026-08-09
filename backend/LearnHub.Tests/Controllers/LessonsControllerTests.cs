using System.Security.Claims;
using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.DTOs.Course;
using LearnHub.Models.DTOs.Progress;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "...")] is enforced by ASP.NET Core's authorization middleware,
    // which never runs when a test constructs LessonsController directly - same documented
    // limitation as CoursesControllerTests.
    public class LessonsControllerTests
    {
        private static (AppDbContext Db, LessonsController Controller) CreateSut(ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock
                .Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<ContentType>()))
                .ReturnsAsync("https://cloudinary.example.com/file.mp4");
            fileUploadMock
                .Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.example.com/certificate.pdf");

            var lessonService = new LessonService(db, fileUploadMock.Object);
            var progressService = new ProgressService(db, new CertificateService(db, fileUploadMock.Object));
            var controller = new LessonsController(lessonService, progressService)
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

        private static Section SeedCourseWithSection(AppDbContext db, long instructorId)
        {
            var course = new Course { InstructorId = instructorId, Title = "Intro to Testing", Description = "Learn how to write unit tests", Status = CourseStatus.Published, CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            db.SaveChanges();
            var section = new Section { CourseId = course.Id, Title = "Section 1", Order = 1 };
            db.Set<Section>().Add(section);
            db.SaveChanges();
            return section;
        }

        private static IFormFile BuildFormFile(string fileName, long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(length);
            return file.Object;
        }

        // ----- POST /api/lessons -----

        [Fact]
        public async Task Create_ValidInput_ReturnsOk()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var section = SeedCourseWithSection(db, instructor.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));
            var dto = new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") };

            var result = await controller.Create(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ----- DELETE /api/lessons/{id} -----

        [Fact]
        public async Task Delete_Owner_ReturnsNoContent()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var section = SeedCourseWithSection(db, instructor.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));
            var created = await controller.Create(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") });
            var lessonId = ((LessonSummaryDto)((OkObjectResult)created).Value!).Id;

            var result = await controller.Delete(lessonId);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NonOwner_Returns403()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var otherInstructor = SeedUser(db, "other@learnhub.com", Role.Instructor);
            var section = SeedCourseWithSection(db, instructor.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));
            var created = await controller.Create(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") });
            var lessonId = ((LessonSummaryDto)((OkObjectResult)created).Value!).Id;

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(otherInstructor.Id, role: "Instructor"));
            var result = await controller.Delete(lessonId);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        // ----- PUT /api/lessons/{id}/progress -----

        [Fact]
        public async Task UpdateProgress_EnrolledStudent_ReturnsOk()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var section = SeedCourseWithSection(db, instructor.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));
            var created = await controller.Create(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") });
            var lessonId = ((LessonSummaryDto)((OkObjectResult)created).Value!).Id;
            db.Enrollments.Add(new Enrollment { StudentId = student.Id, CourseId = section.CourseId, EnrolledAt = DateTime.UtcNow });
            db.SaveChanges();

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));
            var result = await controller.UpdateProgress(lessonId, new UpdateLessonProgressDto { WatchSeconds = 30, IsCompleted = false });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateProgress_NotEnrolled_Returns403()
        {
            var (db, controller) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var section = SeedCourseWithSection(db, instructor.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));
            var created = await controller.Create(new CreateLessonDto { SectionId = section.Id, Title = "Lesson 1", ContentType = ContentType.Video, Duration = 120, File = BuildFormFile("video.mp4") });
            var lessonId = ((LessonSummaryDto)((OkObjectResult)created).Value!).Id;

            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));
            var result = await controller.UpdateProgress(lessonId, new UpdateLessonProgressDto { WatchSeconds = 30, IsCompleted = false });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }
    }
}
