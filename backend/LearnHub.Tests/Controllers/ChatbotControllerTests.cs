using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.DTOs.Chatbot;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearnHub.Tests.Controllers
{
    // [Authorize] is enforced by ASP.NET Core's authorization middleware, which never runs
    // when a test constructs ChatbotController directly - same documented limitation as
    // CoursesControllerTests.
    public class ChatbotControllerTests
    {
        private static (AppDbContext Db, ChatbotController Controller) CreateSut(long userId, string role)
        {
            var db = TestDbContextFactory.Create();
            var geminiMock = new Mock<IGeminiClient>();
            geminiMock
                .Setup(g => g.GenerateReplyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("Mocked tutor reply.");
            var controller = new ChatbotController(new ChatbotService(db, geminiMock.Object))
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(userId, role: role))
            };
            return (db, controller);
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

        private static Course SeedCourse(AppDbContext db, long instructorId)
        {
            var course = new Course
            {
                InstructorId = instructorId,
                Title = "Intro to Testing",
                Description = "Learn how to write unit tests",
                Status = CourseStatus.Published,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        private static void SeedEnrollment(AppDbContext db, long studentId, long courseId)
        {
            db.Enrollments.Add(new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        // ----- POST /api/courses/{courseId}/chat -----

        [Fact]
        public async Task Ask_EnrolledStudent_ReturnsOk()
        {
            var (db, controller) = CreateSut(1, "Student");
            var owner = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, owner.Id);
            SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.Ask(course.Id, new ChatRequestDto { Message = "What is this course about?" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Ask_NotEnrolledStudent_PublishedCourse_ReturnsOk()
        {
            var (db, controller) = CreateSut(1, "Student");
            var owner = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, owner.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.Ask(course.Id, new ChatRequestDto { Message = "Hi" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Ask_NotOwnerNotAdmin_DraftCourse_Returns403()
        {
            var (db, controller) = CreateSut(1, "Student");
            var owner = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, owner.Id);
            course.Status = CourseStatus.Draft;
            db.SaveChanges();
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.Ask(course.Id, new ChatRequestDto { Message = "Hi" });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }
    }
}
