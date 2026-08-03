using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.DTOs.InstructorApplication;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests.Services
{
    public class InstructorApplicationServiceTests
    {
        private static (AppDbContext Db, InstructorApplicationService Sut) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            return (db, new InstructorApplicationService(db));
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

        private static Models.Entities.InstructorApplication SeedApplication(AppDbContext db, long userId, ApplicationStatus status = ApplicationStatus.Pending, DateTime? submittedAt = null)
        {
            var application = new Models.Entities.InstructorApplication
            {
                UserId = userId,
                Message = "I would like to teach.",
                Status = status,
                SubmittedAt = submittedAt ?? DateTime.UtcNow,
            };
            db.InstructorApplications.Add(application);
            db.SaveChanges();
            return application;
        }

        // ----- SubmitAsync -----

        [Fact]
        public async Task SubmitAsync_Student_Succeeds()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var result = await sut.SubmitAsync(student.Id, new SubmitInstructorApplicationDto { Message = "Let me teach!" });

            result.Status.Should().Be(ApplicationStatus.Pending);
            db.InstructorApplications.Should().ContainSingle(a => a.UserId == student.Id && a.Status == ApplicationStatus.Pending);
        }

        [Fact]
        public async Task SubmitAsync_NonStudent_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);

            var act = async () => await sut.SubmitAsync(instructor.Id, new SubmitInstructorApplicationDto { Message = "Let me teach!" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task SubmitAsync_AlreadyHasPendingApplication_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            SeedApplication(db, student.Id, ApplicationStatus.Pending);

            var act = async () => await sut.SubmitAsync(student.Id, new SubmitInstructorApplicationDto { Message = "Let me teach!" });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task SubmitAsync_PreviouslyRejected_AllowsResubmission()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            SeedApplication(db, student.Id, ApplicationStatus.Rejected);

            var result = await sut.SubmitAsync(student.Id, new SubmitInstructorApplicationDto { Message = "Trying again." });

            result.Status.Should().Be(ApplicationStatus.Pending);
            db.InstructorApplications.Where(a => a.UserId == student.Id).Should().HaveCount(2);
        }

        // ----- GetMineAsync -----

        [Fact]
        public async Task GetMineAsync_ReturnsOnlyOwnApplicationsNewestFirst()
        {
            var (db, sut) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var otherStudent = SeedUser(db, "other@learnhub.com", Role.Student);
            SeedApplication(db, otherStudent.Id);
            var older = SeedApplication(db, student.Id, ApplicationStatus.Rejected, DateTime.UtcNow.AddDays(-2));
            var newer = SeedApplication(db, student.Id, ApplicationStatus.Pending, DateTime.UtcNow);

            var result = await sut.GetMineAsync(student.Id);

            result.Select(a => a.Id).Should().ContainInOrder(newer.Id, older.Id);
        }

        // ----- GetAllAsync -----

        [Fact]
        public async Task GetAllAsync_FiltersByStatus()
        {
            var (db, sut) = CreateSut();
            var student1 = SeedUser(db, "student1@learnhub.com", Role.Student);
            var student2 = SeedUser(db, "student2@learnhub.com", Role.Student);
            SeedApplication(db, student1.Id, ApplicationStatus.Pending);
            var approved = SeedApplication(db, student2.Id, ApplicationStatus.Approved);

            var result = await sut.GetAllAsync(1, 20, ApplicationStatus.Approved);

            result.Items.Should().ContainSingle().Which.Id.Should().Be(approved.Id);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_NoStatusFilter_ReturnsAll()
        {
            var (db, sut) = CreateSut();
            var student1 = SeedUser(db, "student1@learnhub.com", Role.Student);
            var student2 = SeedUser(db, "student2@learnhub.com", Role.Student);
            SeedApplication(db, student1.Id, ApplicationStatus.Pending);
            SeedApplication(db, student2.Id, ApplicationStatus.Approved);

            var result = await sut.GetAllAsync(1, 20, null);

            result.TotalCount.Should().Be(2);
        }

        // ----- ApproveAsync -----

        [Fact]
        public async Task ApproveAsync_PendingApplication_FlipsStatusAndRole()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var application = SeedApplication(db, student.Id);

            var result = await sut.ApproveAsync(application.Id, admin.Id);

            result.Status.Should().Be(ApplicationStatus.Approved);
            (await db.Users.FindAsync(student.Id))!.Role.Should().Be(Role.Instructor);
            var updated = await db.InstructorApplications.FindAsync(application.Id);
            updated!.ReviewedByUserId.Should().Be(admin.Id);
            updated.ReviewedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ApproveAsync_AlreadyReviewed_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var application = SeedApplication(db, student.Id, ApplicationStatus.Approved);

            var act = async () => await sut.ApproveAsync(application.Id, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task ApproveAsync_UnknownApplication_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.ApproveAsync(12345, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- RejectAsync -----

        [Fact]
        public async Task RejectAsync_PendingApplication_FlipsStatusOnly()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var application = SeedApplication(db, student.Id);

            var result = await sut.RejectAsync(application.Id, admin.Id);

            result.Status.Should().Be(ApplicationStatus.Rejected);
            (await db.Users.FindAsync(student.Id))!.Role.Should().Be(Role.Student);
        }

        [Fact]
        public async Task RejectAsync_AlreadyReviewed_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var application = SeedApplication(db, student.Id, ApplicationStatus.Rejected);

            var act = async () => await sut.RejectAsync(application.Id, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task RejectAsync_UnknownApplication_ThrowsApiException()
        {
            var (db, sut) = CreateSut();
            var admin = SeedUser(db, "admin@learnhub.com", Role.Admin);

            var act = async () => await sut.RejectAsync(12345, admin.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }
    }
}
