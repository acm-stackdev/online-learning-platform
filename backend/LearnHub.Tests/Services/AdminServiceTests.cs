using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class AdminServiceTests
    {
        private static (AppDbContext Db, AdminService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            return (db, new AdminService(db));
        }

        private static User SeedUser(AppDbContext db, string email, Role role = Role.Student, bool isSuspended = false)
        {
            var user = new User
            {
                Username = email.Split('@')[0],
                Email = email,
                Role = role,
                IsEmailVerified = true,
                IsSuspended = isSuspended,
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

        // ----- GetUsersAsync -----

        [Fact]
        public async Task GetUsersAsync_FiltersBySearch()
        {
            var (db, sut) = CreateSut();
            SeedUser(db, "alice@learnhub.com");
            SeedUser(db, "bob@learnhub.com");

            var result = await sut.GetUsersAsync(1, 20, "alice", null, null);

            result.Items.Should().ContainSingle().Which.Email.Should().Be("alice@learnhub.com");
        }

        [Fact]
        public async Task GetUsersAsync_FiltersByRole()
        {
            var (db, sut) = CreateSut();
            SeedUser(db, "student@learnhub.com", Role.Student);
            SeedUser(db, "instructor@learnhub.com", Role.Instructor);

            var result = await sut.GetUsersAsync(1, 20, null, Role.Instructor, null);

            result.Items.Should().ContainSingle().Which.Role.Should().Be(Role.Instructor);
        }

        [Fact]
        public async Task GetUsersAsync_FiltersBySuspended()
        {
            var (db, sut) = CreateSut();
            SeedUser(db, "active@learnhub.com", isSuspended: false);
            SeedUser(db, "suspended@learnhub.com", isSuspended: true);

            var result = await sut.GetUsersAsync(1, 20, null, null, true);

            result.Items.Should().ContainSingle().Which.Email.Should().Be("suspended@learnhub.com");
        }

        // ----- SuspendUserAsync -----

        [Fact]
        public async Task SuspendUserAsync_ActiveUser_SetsSuspended()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com");

            var result = await sut.SuspendUserAsync(student.Id, admin.Id);

            result.IsSuspended.Should().BeTrue();
            (await db.Users.FindAsync(student.Id))!.IsSuspended.Should().BeTrue();
        }

        [Fact]
        public async Task SuspendUserAsync_AlreadySuspended_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", isSuspended: true);

            var act = async () => await sut.SuspendUserAsync(student.Id, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SuspendUserAsync_Self_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.SuspendUserAsync(admin.Id, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SuspendUserAsync_UnknownId_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.SuspendUserAsync(12345, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- ReinstateUserAsync -----

        [Fact]
        public async Task ReinstateUserAsync_SuspendedUser_ClearsSuspension()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", isSuspended: true);

            var result = await sut.ReinstateUserAsync(student.Id);

            result.IsSuspended.Should().BeFalse();
            (await db.Users.FindAsync(student.Id))!.IsSuspended.Should().BeFalse();
        }

        [Fact]
        public async Task ReinstateUserAsync_NotSuspended_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", isSuspended: false);

            var act = async () => await sut.ReinstateUserAsync(student.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ReinstateUserAsync_UnknownId_ThrowsApiException()
        {
            var (_, sut) = CreateSut();

            var act = async () => await sut.ReinstateUserAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- ChangeRoleAsync -----

        [Fact]
        public async Task ChangeRoleAsync_ValidRole_UpdatesRole()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var result = await sut.ChangeRoleAsync(student.Id, Role.Instructor, admin.Id);

            result.Role.Should().Be(Role.Instructor);
            (await db.Users.FindAsync(student.Id))!.Role.Should().Be(Role.Instructor);
        }

        [Fact]
        public async Task ChangeRoleAsync_CanPromoteToAdmin()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var result = await sut.ChangeRoleAsync(student.Id, Role.Admin, admin.Id);

            result.Role.Should().Be(Role.Admin);
        }

        [Fact]
        public async Task ChangeRoleAsync_NullRole_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.ChangeRoleAsync(student.Id, null, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ChangeRoleAsync_Self_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.ChangeRoleAsync(admin.Id, Role.Student, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ChangeRoleAsync_UnknownId_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.ChangeRoleAsync(12345, Role.Instructor, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- GetPlatformStatsAsync -----

        [Fact]
        public async Task GetPlatformStatsAsync_ReturnsAccurateCounts()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student1 = SeedUser(db, "student1@learnhub.com", Role.Student);
            var student2 = SeedUser(db, "student2@learnhub.com", Role.Student, isSuspended: true);

            var publishedCourse = SeedCourse(db, instructor.Id, CourseStatus.Published);
            SeedCourse(db, instructor.Id, CourseStatus.Draft);
            SeedCourse(db, instructor.Id, CourseStatus.PendingApproval);

            SeedEnrollment(db, student1.Id, publishedCourse.Id, completedAt: DateTime.UtcNow);
            SeedEnrollment(db, student2.Id, publishedCourse.Id, completedAt: null);

            var result = await sut.GetPlatformStatsAsync();

            result.TotalUsers.Should().Be(4);
            result.StudentCount.Should().Be(2);
            result.InstructorCount.Should().Be(1);
            result.AdminCount.Should().Be(1);
            result.SuspendedCount.Should().Be(1);

            result.TotalCourses.Should().Be(3);
            result.PublishedCourseCount.Should().Be(1);
            result.DraftCourseCount.Should().Be(1);
            result.PendingApprovalCourseCount.Should().Be(1);
            result.RejectedCourseCount.Should().Be(0);

            result.TotalEnrollments.Should().Be(2);
            result.CompletedEnrollmentCount.Should().Be(1);
            result.InProgressEnrollmentCount.Should().Be(1);
        }
    }
}
