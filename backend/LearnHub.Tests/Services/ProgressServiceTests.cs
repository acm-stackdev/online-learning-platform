using FluentAssertions;
using LearnHub.Data;
using LearnHub.Models.Entities;
using LearnHub.Models.DTOs.Progress;
using LearnHub.Services;
using LearnHub.Tests.Fixtures;
using Moq;

namespace LearnHub.Tests.Services
{
    public class ProgressServiceTests
    {
        private static (AppDbContext Db, ProgressService Sut, Mock<IFileUploadService> FileUploadMock) CreateSut()
        {
            var db = TestDbContextFactory.Create();
            var fileUploadMock = new Mock<IFileUploadService>();
            fileUploadMock
                .Setup(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync("https://cloudinary.example.com/certificate.pdf");
            var certificateService = new CertificateService(db, fileUploadMock.Object);
            var sut = new ProgressService(db, certificateService);
            return (db, sut, fileUploadMock);
        }

        private static User SeedUser(AppDbContext db, string email, Role role)
        {
            var user = new User { Username = email.Split('@')[0], Email = email, Role = role, IsEmailVerified = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static (Course Course, List<Lesson> Lessons) SeedCourseWithLessons(AppDbContext db, long instructorId, int lessonCount)
        {
            var course = new Course { InstructorId = instructorId, Title = "Intro to Testing", Description = "Learn how to write unit tests", Status = CourseStatus.Published, CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            db.SaveChanges();

            var section = new Section { CourseId = course.Id, Title = "Section 1", Order = 1 };
            db.Set<Section>().Add(section);
            db.SaveChanges();

            var lessons = new List<Lesson>();
            for (var i = 0; i < lessonCount; i++)
            {
                var lesson = new Lesson
                {
                    SectionId = section.Id,
                    Title = $"Lesson {i + 1}",
                    ContentType = ContentType.Video,
                    ContentUrl = "https://example.com/video.mp4",
                    Duration = 60,
                    Order = i + 1,
                };
                db.Lessons.Add(lesson);
                lessons.Add(lesson);
            }
            db.SaveChanges();

            return (course, lessons);
        }

        private static Enrollment SeedEnrollment(AppDbContext db, long studentId, long courseId)
        {
            var enrollment = new Enrollment { StudentId = studentId, CourseId = courseId, EnrolledAt = DateTime.UtcNow };
            db.Enrollments.Add(enrollment);
            db.SaveChanges();
            return enrollment;
        }

        // ----- UpdateProgressAsync -----

        [Fact]
        public async Task UpdateProgressAsync_Enrolled_CreatesProgressRecord()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var (course, lessons) = SeedCourseWithLessons(db, instructor.Id, 2);
            SeedEnrollment(db, student.Id, course.Id);

            var result = await sut.UpdateProgressAsync(lessons[0].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 30, IsCompleted = false });

            result.IsCompleted.Should().BeFalse();
            result.WatchSeconds.Should().Be(30);
        }

        [Fact]
        public async Task UpdateProgressAsync_LessonNotFound_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.UpdateProgressAsync(12345, student.Id, new UpdateLessonProgressDto { WatchSeconds = 30, IsCompleted = false });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateProgressAsync_NotEnrolled_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var (course, lessons) = SeedCourseWithLessons(db, instructor.Id, 1);

            var act = async () => await sut.UpdateProgressAsync(lessons[0].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 30, IsCompleted = false });

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateProgressAsync_CompletingFinalLesson_MarksEnrollmentCompleteAndIssuesCertificate()
        {
            var (db, sut, fileUploadMock) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var (course, lessons) = SeedCourseWithLessons(db, instructor.Id, 2);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.UpdateProgressAsync(lessons[0].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 60, IsCompleted = true });

            await sut.UpdateProgressAsync(lessons[1].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 60, IsCompleted = true });

            db.Enrollments.First(e => e.Id == enrollment.Id).CompletedAt.Should().NotBeNull();
            db.Certificates.Any(c => c.EnrollmentId == enrollment.Id).Should().BeTrue();
            fileUploadMock.Verify(f => f.UploadRawAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProgressAsync_NotAllLessonsComplete_DoesNotIssueCertificate()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var (course, lessons) = SeedCourseWithLessons(db, instructor.Id, 2);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            await sut.UpdateProgressAsync(lessons[0].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 60, IsCompleted = true });

            db.Enrollments.First(e => e.Id == enrollment.Id).CompletedAt.Should().BeNull();
            db.Certificates.Any(c => c.EnrollmentId == enrollment.Id).Should().BeFalse();
        }

        // ----- GetEnrollmentProgressAsync -----

        [Fact]
        public async Task GetEnrollmentProgressAsync_CalculatesPercentComplete()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var (course, lessons) = SeedCourseWithLessons(db, instructor.Id, 4);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);
            await sut.UpdateProgressAsync(lessons[0].Id, student.Id, new UpdateLessonProgressDto { WatchSeconds = 60, IsCompleted = true });

            var result = await sut.GetEnrollmentProgressAsync(enrollment.Id, student.Id);

            result.TotalLessons.Should().Be(4);
            result.CompletedLessons.Should().Be(1);
            result.PercentComplete.Should().Be(25.0);
        }

        [Fact]
        public async Task GetEnrollmentProgressAsync_UnknownId_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var student = SeedUser(db, "student@learnhub.com", Role.Student);

            var act = async () => await sut.GetEnrollmentProgressAsync(12345, student.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetEnrollmentProgressAsync_NotYourEnrollment_ThrowsApiException()
        {
            var (db, sut, _) = CreateSut();
            var instructor = SeedUser(db, "instructor@learnhub.com", Role.Instructor);
            var student = SeedUser(db, "student@learnhub.com", Role.Student);
            var otherStudent = SeedUser(db, "other@learnhub.com", Role.Student);
            var (course, _) = SeedCourseWithLessons(db, instructor.Id, 1);
            var enrollment = SeedEnrollment(db, student.Id, course.Id);

            var act = async () => await sut.GetEnrollmentProgressAsync(enrollment.Id, otherStudent.Id);

            var ex = await act.Should().ThrowAsync<ApiException>();
            ex.Which.StatusCode.Should().Be(403);
        }
    }
}
