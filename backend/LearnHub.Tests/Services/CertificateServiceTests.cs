using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Moq;

namespace LearnHub.Tests.Services
{
    public class CertificateServiceTests
    {
        private static (AppDbContext Db, CertificateService Sut, Mock<IFileUploadService> FileUploadMock) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock
                .Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            var sut = new CertificateService(db, fileUploadMock.Object);
            return (db, sut, fileUploadMock);
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

        // ----- IssueForEnrollmentAsync -----

        [Fact]
        public async Task IssueForEnrollmentAsync_ValidEnrollment_CreatesCertificate()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);

            await sut.IssueForEnrollmentAsync(enrollment.Id);

            var certificate = db.Certificates.Single(c => c.EnrollmentId == enrollment.Id);
            certificate.CertificateUrl.Should().Be("https://cloudinary.example.com/certificate.pdf");
            fileUploadMock.Verify(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task IssueForEnrollmentAsync_AlreadyIssued_DoesNotIssueAgain()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            await sut.IssueForEnrollmentAsync(enrollment.Id);

            await sut.IssueForEnrollmentAsync(enrollment.Id);

            db.Certificates.Count(c => c.EnrollmentId == enrollment.Id).Should().Be(1);
            fileUploadMock.Verify(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task IssueForEnrollmentAsync_UnknownEnrollment_ThrowsApiException()
        {
            var (_, sut, _) = CreateSut();

            var act = async () => await sut.IssueForEnrollmentAsync(12345);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        // ----- GetForEnrollmentAsync -----

        [Fact]
        public async Task GetForEnrollmentAsync_Owner_ReturnsCertificate()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            await sut.IssueForEnrollmentAsync(enrollment.Id);

            var result = await sut.GetForEnrollmentAsync(enrollment.Id, student.Id, isAdmin: false);

            result.EnrollmentId.Should().Be(enrollment.Id);
            result.StudentUsername.Should().Be(student.Username);
        }

        [Fact]
        public async Task GetForEnrollmentAsync_Admin_ReturnsCertificate()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            await sut.IssueForEnrollmentAsync(enrollment.Id);

            var result = await sut.GetForEnrollmentAsync(enrollment.Id, requesterId: 999, isAdmin: true);

            result.EnrollmentId.Should().Be(enrollment.Id);
        }

        [Fact]
        public async Task GetForEnrollmentAsync_NotTheStudent_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var otherStudent = SeedUser(db, "other@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);
            await sut.IssueForEnrollmentAsync(enrollment.Id);

            var act = async () => await sut.GetForEnrollmentAsync(enrollment.Id, otherStudent.Id, isAdmin: false);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetForEnrollmentAsync_NoCertificateIssued_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var enrollment = SeedCompletedEnrollment(db, student.Id, instructor.Id);

            var act = async () => await sut.GetForEnrollmentAsync(enrollment.Id, student.Id, isAdmin: false);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }
    }
}
