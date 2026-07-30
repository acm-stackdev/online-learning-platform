using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class EnrollmentServiceTests
    {
        private static (AppDbContext Db, EnrollmentService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            return (db, new EnrollmentService(db));
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

        private static Course SeedCourse(AppDbContext db, long instructorId, CourseStatus status = CourseStatus.Published)
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

        // ----- EnrollAsync -----

        [Fact]
        public async Task EnrollAsync_Student_Succeeds()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);

            var result = await sut.EnrollAsync(course.Id, student.Id, Role.Student);

            result.CompletedAt.Should().BeNull();
            db.Enrollments.Should().ContainSingle(e => e.StudentId == student.Id && e.CourseId == course.Id);
        }

        [Fact]
        public async Task EnrollAsync_InstructorEnrollingInAnotherInstructorsCourse_Succeeds()
        {
            var (db, sut) = CreateSut();
            var owningInstructor = SeedUser(db, "owner@learnhub.com", Role.Instructor);
            var otherInstructor = SeedUser(db, "other@learnhub.com", Role.Instructor);
            var course = SeedCourse(db, owningInstructor.Id);

            var result = await sut.EnrollAsync(course.Id, otherInstructor.Id, Role.Instructor);

            result.CompletedAt.Should().BeNull();
            db.Enrollments.Should().ContainSingle(e => e.StudentId == otherInstructor.Id && e.CourseId == course.Id);
        }

        [Fact]
        public async Task EnrollAsync_Admin_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var course = SeedCourse(db, instructor.Id);

            var act = async () => await sut.EnrollAsync(course.Id, admin.Id, Role.Admin);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task EnrollAsync_InstructorOwnCourse_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var course = SeedCourse(db, instructor.Id);

            var act = async () => await sut.EnrollAsync(course.Id, instructor.Id, Role.Instructor);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task EnrollAsync_DuplicateEnrollment_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id);
            await sut.EnrollAsync(course.Id, student.Id, Role.Student);

            var act = async () => await sut.EnrollAsync(course.Id, student.Id, Role.Student);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task EnrollAsync_UnpublishedCourse_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var course = SeedCourse(db, instructor.Id, CourseStatus.Draft);

            var act = async () => await sut.EnrollAsync(course.Id, student.Id, Role.Student);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task EnrollAsync_UnknownCourse_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.EnrollAsync(12345, student.Id, Role.Student);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }
    }
}
