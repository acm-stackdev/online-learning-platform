using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class FakePresenceTracker : IPresenceTracker
    {
        private readonly HashSet<long> _onlineUserIds = new();

        public void SetOnline(long userId) => _onlineUserIds.Add(userId);

        public bool AddConnection(long userId, string connectionId)
        {
            var wasOffline = !_onlineUserIds.Contains(userId);
            _onlineUserIds.Add(userId);
            return wasOffline;
        }

        public bool RemoveConnection(long userId, string connectionId)
        {
            _onlineUserIds.Remove(userId);
            return true;
        }

        public bool IsOnline(long userId) => _onlineUserIds.Contains(userId);
    }

    public class MessagingServiceTests
    {
        private static (AppDbContext Db, MessagingService Sut, FakePresenceTracker Presence) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var presence = new FakePresenceTracker();
            return (db, new MessagingService(db, presence), presence);
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

        // ----- SendMessageAsync -----

        [Fact]
        public async Task SendMessageAsync_FirstMessage_CreatesConversationLazily()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            await sut.SendMessageAsync(enrollment.Id, student.Id, "Hello!");

            db.Conversations.Should().ContainSingle(c => c.EnrollmentId == enrollment.Id);
            db.Messages.Should().ContainSingle(m => m.Content == "Hello!");
        }

        [Fact]
        public async Task SendMessageAsync_SecondMessage_ReusesExistingConversation()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            await sut.SendMessageAsync(enrollment.Id, student.Id, "First");
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Second");

            db.Conversations.Should().HaveCount(1);
            db.Messages.Should().HaveCount(2);
            db.Messages.Select(m => m.ConversationId).Distinct().Should().ContainSingle();
        }

        [Fact]
        public async Task SendMessageAsync_OwningInstructor_Succeeds()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var result = await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Hi student");

            result.Message.SenderId.Should().Be(instructor.Id);
        }

        [Fact]
        public async Task SendMessageAsync_NonParticipant_ThrowsForbidden()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var stranger = SeedUser(db, "stranger@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var act = async () => await sut.SendMessageAsync(enrollment.Id, stranger.Id, "Hi");

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task SendMessageAsync_UnknownEnrollmentId_ThrowsNotFound()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.SendMessageAsync(12345, student.Id, "Hi");

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendMessageAsync_EmptyOrWhitespaceContent_ThrowsBadRequest(string content)
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var act = async () => await sut.SendMessageAsync(enrollment.Id, student.Id, content);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SendMessageAsync_ReturnsCorrectRecipientId_ForBothDirections()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var fromStudent = await sut.SendMessageAsync(enrollment.Id, student.Id, "Hi");
            var fromInstructor = await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Hello back");

            fromStudent.RecipientId.Should().Be(instructor.Id);
            fromInstructor.RecipientId.Should().Be(student.Id);
        }

        // ----- GetMyConversationsAsync -----

        [Fact]
        public async Task GetMyConversationsAsync_IncludesEnrollmentsWithNoMessagesYet()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var result = await sut.GetMyConversationsAsync(student.Id);

            result.Should().ContainSingle();
            result[0].ConversationId.Should().BeNull();
            result[0].UnreadCount.Should().Be(0);
            result[0].LastMessagePreview.Should().BeNull();
        }

        [Fact]
        public async Task GetMyConversationsAsync_UnreadCount_ExcludesOwnSentMessages()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            await sut.SendMessageAsync(enrollment.Id, student.Id, "From student");
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "From instructor 1");
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "From instructor 2");

            var result = await sut.GetMyConversationsAsync(student.Id);

            result[0].UnreadCount.Should().Be(2);
        }

        [Fact]
        public async Task GetMyConversationsAsync_DualRoleUser_CombinesBothDirections()
        {
            var (db, sut, _) = CreateSut();
            var dualRoleUser = SeedUser(db, "dual@learnhub.com", Role.Instructor);
            var otherInstructor = SeedUser(db, "other-instructor@learnhub.com", Role.Instructor);
            var otherStudent = SeedUser(db, "other-student@learnhub.com", Role.Student);

            // dualRoleUser is a *student* on someone else's course
            var otherCourse = SeedCourse(db, otherInstructor.Id, "Other's Course");
            var enrollmentAsStudent = SeedEnrollment(db, dualRoleUser.Id, otherCourse.Id);

            // dualRoleUser is the *instructor* on their own course
            var ownCourse = SeedCourse(db, dualRoleUser.Id, "Dual's Course");
            var enrollmentAsInstructor = SeedEnrollment(db, otherStudent.Id, ownCourse.Id);

            var result = await sut.GetMyConversationsAsync(dualRoleUser.Id);

            result.Should().HaveCount(2);
            result.Should().ContainSingle(c => c.EnrollmentId == enrollmentAsStudent.Id && c.OtherPartyId == otherInstructor.Id);
            result.Should().ContainSingle(c => c.EnrollmentId == enrollmentAsInstructor.Id && c.OtherPartyId == otherStudent.Id);
        }

        [Fact]
        public async Task GetMyConversationsAsync_OrdersByMostRecentActivityDescending()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var studentA = SeedUser(db, "student-a@learnhub.com", Role.Student);
            var studentB = SeedUser(db, "student-b@learnhub.com", Role.Student);
            var courseA = SeedCourse(db, instructor.Id, "Course A");
            var courseB = SeedCourse(db, instructor.Id, "Course B");
            var enrollmentA = SeedEnrollment(db, studentA.Id, courseA.Id);
            var enrollmentB = SeedEnrollment(db, studentB.Id, courseB.Id);

            await sut.SendMessageAsync(enrollmentA.Id, studentA.Id, "Older message");
            await Task.Delay(10);
            await sut.SendMessageAsync(enrollmentB.Id, studentB.Id, "Newer message");

            var result = await sut.GetMyConversationsAsync(instructor.Id);

            result.Select(c => c.EnrollmentId).Should().ContainInOrder(enrollmentB.Id, enrollmentA.Id);
        }

        // ----- GetConversationHistoryAsync -----

        [Fact]
        public async Task GetConversationHistoryAsync_Participant_ReturnsPagedResultsNewestFirst()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            await sut.SendMessageAsync(enrollment.Id, student.Id, "First");
            await Task.Delay(10);
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Second");

            var conversationId = db.Conversations.Single().Id;
            var result = await sut.GetConversationHistoryAsync(conversationId, student.Id, page: 1, pageSize: 20);

            result.Items.Should().HaveCount(2);
            result.Items[0].Content.Should().Be("Second");
            result.Items[1].Content.Should().Be("First");
        }

        [Fact]
        public async Task GetConversationHistoryAsync_NonParticipant_ThrowsForbidden()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var stranger = SeedUser(db, "stranger@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, student.Id, "Hi");
            var conversationId = db.Conversations.Single().Id;

            var act = async () => await sut.GetConversationHistoryAsync(conversationId, stranger.Id, 1, 20);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_UnknownConversationId_ThrowsNotFound()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.GetConversationHistoryAsync(12345, student.Id, 1, 20);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_PagingBoundaries_TotalCountReflectsAllMessages()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            for (var i = 0; i < 5; i++)
                await sut.SendMessageAsync(enrollment.Id, student.Id, $"Message {i}");

            var conversationId = db.Conversations.Single().Id;
            var result = await sut.GetConversationHistoryAsync(conversationId, student.Id, page: 1, pageSize: 2);

            result.TotalCount.Should().Be(5);
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_DoesNotMutateReadAt()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, student.Id, "Hi");
            var conversationId = db.Conversations.Single().Id;

            await sut.GetConversationHistoryAsync(conversationId, instructor.Id, 1, 20);

            db.Messages.Should().OnlyContain(m => m.ReadAt == null);
        }

        // ----- MarkConversationReadAsync -----

        [Fact]
        public async Task MarkConversationReadAsync_MarksOnlyOtherPartysUnreadMessages()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, student.Id, "From student");
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "From instructor");
            var conversationId = db.Conversations.Single().Id;

            await sut.MarkConversationReadAsync(conversationId, student.Id);

            db.Messages.Single(m => m.Content == "From instructor").ReadAt.Should().NotBeNull();
            db.Messages.Single(m => m.Content == "From student").ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task MarkConversationReadAsync_NonParticipant_ThrowsForbidden()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var stranger = SeedUser(db, "stranger@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, student.Id, "Hi");
            var conversationId = db.Conversations.Single().Id;

            var act = async () => await sut.MarkConversationReadAsync(conversationId, stranger.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task MarkConversationReadAsync_UnknownConversationId_ThrowsNotFound()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.MarkConversationReadAsync(12345, student.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task MarkConversationReadAsync_SecondCallWithNothingUnread_ReturnsEmptyMessageIds_DoesNotThrow()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Hi");
            var conversationId = db.Conversations.Single().Id;

            await sut.MarkConversationReadAsync(conversationId, student.Id);
            var second = await sut.MarkConversationReadAsync(conversationId, student.Id);

            second.MessageIds.Should().BeEmpty();
        }

        [Fact]
        public async Task MarkConversationReadAsync_ReturnsOtherPartyId_ForHubBroadcastTargeting()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.SendMessageAsync(enrollment.Id, instructor.Id, "Hi");
            var conversationId = db.Conversations.Single().Id;

            var result = await sut.MarkConversationReadAsync(conversationId, student.Id);

            result.OtherPartyId.Should().Be(instructor.Id);
        }

        // ----- GetContactUserIdsAsync -----

        [Fact]
        public async Task GetContactUserIdsAsync_DedupesSameOtherPartyAcrossMultipleCourses()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var courseA = SeedCourse(db, instructor.Id, "Course A");
            var courseB = SeedCourse(db, instructor.Id, "Course B");
            SeedEnrollment(db, student.Id, courseA.Id);
            SeedEnrollment(db, student.Id, courseB.Id);

            var contacts = await sut.GetContactUserIdsAsync(student.Id);

            contacts.Should().ContainSingle().Which.Should().Be(instructor.Id);
        }

        [Fact]
        public async Task GetContactUserIdsAsync_DualRoleUser_CombinesBothDirections()
        {
            var (db, sut, _) = CreateSut();
            var dualRoleUser = SeedUser(db, "dual@learnhub.com", Role.Instructor);
            var otherInstructor = SeedUser(db, "other-instructor@learnhub.com", Role.Instructor);
            var otherStudent = SeedUser(db, "other-student@learnhub.com", Role.Student);
            var otherCourse = SeedCourse(db, otherInstructor.Id, "Other's Course");
            SeedEnrollment(db, dualRoleUser.Id, otherCourse.Id);
            var ownCourse = SeedCourse(db, dualRoleUser.Id, "Dual's Course");
            SeedEnrollment(db, otherStudent.Id, ownCourse.Id);

            var contacts = await sut.GetContactUserIdsAsync(dualRoleUser.Id);

            contacts.Should().BeEquivalentTo(new[] { otherInstructor.Id, otherStudent.Id });
        }

        // ----- Presence helpers -----

        [Fact]
        public async Task SetPresenceStatusAsync_UpdatesStoredStatus()
        {
            var (db, sut, _) = CreateSut();
            var user = SeedUser(db, "user@learnhub.com", Role.Student);

            await sut.SetPresenceStatusAsync(user.Id, PresenceStatus.Busy);

            (await db.Users.FindAsync(user.Id))!.PresenceStatus.Should().Be(PresenceStatus.Busy);
        }

        [Fact]
        public async Task UpdateLastActiveAsync_SetsAndReturnsUtcNowTimestamp()
        {
            var (db, sut, _) = CreateSut();
            var user = SeedUser(db, "user@learnhub.com", Role.Student);
            var before = DateTime.UtcNow;

            var returned = await sut.UpdateLastActiveAsync(user.Id);

            returned.Should().BeOnOrAfter(before);
            (await db.Users.FindAsync(user.Id))!.LastActiveAt.Should().Be(returned);
        }

        [Fact]
        public async Task GetStoredPresenceStatusAsync_UnknownUser_ThrowsNotFound()
        {
            var (db, sut, _) = CreateSut();

            var act = async () => await sut.GetStoredPresenceStatusAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }
    }
}
