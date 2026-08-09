using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.DTOs.Chatbot;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Moq;

namespace LearnHub.Tests.Services
{
    public class ChatbotServiceTests
    {
        private static (AppDbContext Db, ChatbotService Sut, Mock<IGeminiClient> GeminiMock) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var geminiMock = new Mock<IGeminiClient>();
            geminiMock
                .Setup(g => g.GenerateReplyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("Mocked tutor reply.");
            var sut = new ChatbotService(db, geminiMock.Object);
            return (db, sut, geminiMock);
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

            var section = new Section { CourseId = course.Id, Title = "Section 1", Order = 1 };
            db.Set<Section>().Add(section);
            db.SaveChanges();

            db.Lessons.Add(new Lesson
            {
                SectionId = section.Id,
                Title = "Lesson 1",
                ContentType = ContentType.Video,
                ContentUrl = "https://example.com/video.mp4",
                Duration = 10,
                Order = 1,
            });
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

        // ----- AskAsync -----

        [Fact]
        public async Task AskAsync_EnrolledStudent_ReturnsReply()
        {
            var (db, sut, geminiMock) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            SeedEnrollment(db, student.Id, course.Id);

            var result = await sut.AskAsync(course.Id, student.Id, isAdmin: false, new ChatRequestDto { Message = "What is this course about?" });

            result.Reply.Should().Be("Mocked tutor reply.");
            geminiMock.Verify(g => g.GenerateReplyAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessageDto>>(), "What is this course about?"), Times.Once);
        }

        [Fact]
        public async Task AskAsync_OwningInstructor_ReturnsReply()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var course = SeedCourse(db, instructor.Id);

            var result = await sut.AskAsync(course.Id, instructor.Id, isAdmin: false, new ChatRequestDto { Message = "Hi" });

            result.Reply.Should().Be("Mocked tutor reply.");
        }

        [Fact]
        public async Task AskAsync_Admin_ReturnsReply()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var course = SeedCourse(db, instructor.Id);

            var result = await sut.AskAsync(course.Id, 999, isAdmin: true, new ChatRequestDto { Message = "Hi" });

            result.Reply.Should().Be("Mocked tutor reply.");
        }

        [Fact]
        public async Task AskAsync_NotEnrolledStudent_PublishedCourse_ReturnsReply()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, instructor.Id);

            var result = await sut.AskAsync(course.Id, student.Id, isAdmin: false, new ChatRequestDto { Message = "Hi" });

            result.Reply.Should().Be("Mocked tutor reply.");
        }

        [Fact]
        public async Task AskAsync_NotOwnerNotAdmin_DraftCourse_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            course.Status = CourseStatus.Draft;
            db.SaveChanges();

            var act = async () => await sut.AskAsync(course.Id, student.Id, isAdmin: false, new ChatRequestDto { Message = "Hi" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task AskAsync_UnknownCourse_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com");

            var act = async () => await sut.AskAsync(12345, student.Id, isAdmin: false, new ChatRequestDto { Message = "Hi" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AskAsync_WithHistory_PassesHistoryThroughUnchanged()
        {
            var (db, sut, geminiMock) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com");
            var course = SeedCourse(db, instructor.Id);
            SeedEnrollment(db, student.Id, course.Id);
            var history = new List<ChatMessageDto>
            {
                new() { Role = "user", Content = "What is this course about?" },
                new() { Role = "model", Content = "It's an intro to testing." },
            };

            await sut.AskAsync(course.Id, student.Id, isAdmin: false, new ChatRequestDto { Message = "Tell me more.", History = history });

            geminiMock.Verify(g => g.GenerateReplyAsync(It.IsAny<string>(), history, "Tell me more."), Times.Once);
        }
    }
}
