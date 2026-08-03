using FluentAssertions;
using LearnHub.Controllers;
using LearnHub.Data;
using LearnHub.Models.DTOs.Common;
using LearnHub.Models.DTOs.Messaging;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using LearnHub.Tests.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Controllers
{
    // [Authorize(Roles = "...")] is enforced by ASP.NET Core's authorization middleware,
    // which never runs when a test constructs MessagingController directly and calls an
    // action method. These tests only prove "given a principal with this role, the action
    // body behaves correctly" - they do NOT prove the wrong role is actually rejected
    // before reaching the method. That needs a WebApplicationFactory integration test,
    // same documented limitation as AuthControllerTests.Me() and CoursesControllerTests.
    public class MessagingControllerTests
    {
        private static (AppDbContext Db, MessagingController Controller, MessagingService Service) CreateSut(System.Security.Claims.ClaimsPrincipal? user = null)
        {
            var db = TestDbContextFactory.Create();
            var service = new MessagingService(db, new FakePresenceTracker());
            var controller = new MessagingController(service)
            {
                ControllerContext = ControllerTestHelpers.BuildControllerContext(user)
            };
            return (db, controller, service);
        }

        private static User SeedUser(AppDbContext db, string email, Role role)
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

        private static Course SeedCourse(AppDbContext db, long instructorId, string title = "Intro to Testing")
        {
            var course = new Course
            {
                InstructorId = instructorId,
                Title = title,
                Description = "Learn how to write unit tests",
                Status = CourseStatus.Published,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            db.SaveChanges();
            return course;
        }

        private static Enrollment SeedEnrollment(AppDbContext db, long studentId, long courseId)
        {
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
            };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
            return enrollment;
        }

        // ----- GET /api/messaging/conversations -----

        [Fact]
        public async Task GetMyConversations_EnrollmentWithoutMessages_ReturnsOkWithUnstartedConversation()
        {
            var (db, controller, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            SeedEnrollment(db, student.Id, course.Id);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetMyConversations();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var items = ok.Value.Should().BeOfType<List<ConversationListItemDto>>().Subject;
            items.Should().ContainSingle();
            items[0].ConversationId.Should().BeNull();
            items[0].UnreadCount.Should().Be(0);
        }

        [Fact]
        public async Task GetMyConversations_NoEnrollments_ReturnsEmptyList()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetMyConversations();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<List<ConversationListItemDto>>().Which.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyConversations_AfterMessageSent_ReflectsLastMessage()
        {
            var (db, controller, service) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await service.SendMessageAsync(enrollment.Id, student.Id, "Hello instructor!");
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));

            var result = await controller.GetMyConversations();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var items = ok.Value.Should().BeOfType<List<ConversationListItemDto>>().Subject;
            items.Should().ContainSingle();
            items[0].ConversationId.Should().NotBeNull();
            items[0].LastMessagePreview.Should().Be("Hello instructor!");
            items[0].UnreadCount.Should().Be(1);
        }

        // ----- GET /api/messaging/conversations/{conversationId}/messages -----

        [Fact]
        public async Task GetHistory_Student_ReturnsOkWithPagedMessages()
        {
            var (db, controller, service) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            var sendResult = await service.SendMessageAsync(enrollment.Id, student.Id, "Hi!");
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetHistory(sendResult.Message.ConversationId);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = ok.Value.Should().BeOfType<PagedResult<MessageDto>>().Subject;
            paged.Items.Should().ContainSingle(m => m.Content == "Hi!");
        }

        [Fact]
        public async Task GetHistory_OwningInstructor_ReturnsOk()
        {
            var (db, controller, service) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            var sendResult = await service.SendMessageAsync(enrollment.Id, student.Id, "Hi!");
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(instructor.Id, role: "Instructor"));

            var result = await controller.GetHistory(sendResult.Message.ConversationId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetHistory_NonParticipant_Returns403()
        {
            var (db, controller, service) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var outsider = SeedUser(db, "outsider@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            var sendResult = await service.SendMessageAsync(enrollment.Id, student.Id, "Hi!");
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(outsider.Id, role: "Student"));

            var result = await controller.GetHistory(sendResult.Message.ConversationId);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetHistory_UnknownConversationId_Returns404()
        {
            var (db, controller, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetHistory(12345);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetHistory_CustomPageAndPageSize_ReturnsRequestedPage()
        {
            var (db, controller, service) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            var first = await service.SendMessageAsync(enrollment.Id, student.Id, "First");
            await Task.Delay(10);
            await service.SendMessageAsync(enrollment.Id, instructor.Id, "Second");
            await Task.Delay(10);
            await service.SendMessageAsync(enrollment.Id, student.Id, "Third");
            controller.ControllerContext = ControllerTestHelpers.BuildControllerContext(ControllerTestHelpers.BuildUserPrincipal(student.Id, role: "Student"));

            var result = await controller.GetHistory(first.Message.ConversationId, page: 2, pageSize: 1);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var paged = ok.Value.Should().BeOfType<PagedResult<MessageDto>>().Subject;
            paged.Page.Should().Be(2);
            paged.Items.Should().ContainSingle();
            paged.Items[0].Content.Should().Be("Second");
        }
    }
}
